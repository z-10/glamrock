// Synchronous bridge between worker and main thread using SharedArrayBuffer.
// Worker sends a request, blocks with Atomics.wait, main thread processes and signals back.

// Shared memory layout:
// Int32Array[0] = signal flag (0=idle, 1=request pending, 2=response ready)
// Int32Array[1] = request type (1=track call)
// The rest = string data passed via TextEncoder/TextDecoder in a shared Uint8Array

const SIGNAL_IDLE = 0;
const SIGNAL_REQUEST = 1;
const SIGNAL_RESPONSE = 2;
const HEADER_INTS = 4;         // signal, type, requestLen, responseLen
const BUFFER_SIZE = 64 * 1024; // 64KB for string data

let sharedBuffer = null;
let signalArray = null;
let dataArray = null;

export function createBridge() {
	sharedBuffer = new SharedArrayBuffer(HEADER_INTS * 4 + BUFFER_SIZE);
	signalArray = new Int32Array(sharedBuffer, 0, HEADER_INTS);
	dataArray = new Uint8Array(sharedBuffer, HEADER_INTS * 4, BUFFER_SIZE);
	return sharedBuffer;
}

export function getSharedBuffer() {
	return sharedBuffer;
}

// --- Main thread side ---

export function initMainSide(buffer, handler) {
	const signal = new Int32Array(buffer, 0, HEADER_INTS);
	const data = new Uint8Array(buffer, HEADER_INTS * 4, BUFFER_SIZE);
	const encoder = new TextEncoder();
	const decoder = new TextDecoder();

	function poll() {
		if (Atomics.load(signal, 0) === SIGNAL_REQUEST) {
			const reqLen = Atomics.load(signal, 2);
			const reqStr = decoder.decode(data.slice(0, reqLen));
			const req = JSON.parse(reqStr);

			let response;
			try {
				response = handler(req.trackName, req.argsJson);
			} catch (e) {
				response = JSON.stringify({ result: null });
			}

			const respBytes = encoder.encode(response);
			data.set(respBytes);
			Atomics.store(signal, 3, respBytes.length);
			Atomics.store(signal, 0, SIGNAL_RESPONSE);
			Atomics.notify(signal, 0);
		}
		requestAnimationFrame(poll);
	}
	requestAnimationFrame(poll);
}

// --- Worker side ---

export function initWorkerSide(buffer) {
	signalArray = new Int32Array(buffer, 0, HEADER_INTS);
	dataArray = new Uint8Array(buffer, HEADER_INTS * 4, BUFFER_SIZE);
}

export function callMainThread(trackName, argsJson) {
	const encoder = new TextEncoder();
	const decoder = new TextDecoder();

	const reqStr = JSON.stringify({ trackName, argsJson });
	const reqBytes = encoder.encode(reqStr);
	dataArray.set(reqBytes);
	Atomics.store(signalArray, 2, reqBytes.length);
	Atomics.store(signalArray, 0, SIGNAL_REQUEST);
	Atomics.notify(signalArray, 0);

	// Block until main thread responds
	Atomics.wait(signalArray, 0, SIGNAL_REQUEST);

	const respLen = Atomics.load(signalArray, 3);
	const respStr = decoder.decode(dataArray.slice(0, respLen));
	Atomics.store(signalArray, 0, SIGNAL_IDLE);

	return respStr;
}
