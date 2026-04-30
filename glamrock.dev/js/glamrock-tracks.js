// Canvas and Audio track implementations for the GlamRock playground.
// These are called by the WASM runtime via the trackHandler callback.

let canvas = null;
let ctx = null;
let canvasWidth = 400;
let canvasHeight = 300;

// Audio state
let audioCtx = null;
let tempo = 120; // BPM
let volume = 0.5;
let instrument = 'sine'; // oscillator type: sine, square, sawtooth, triangle

function ensureAudio() {
	if (!audioCtx) audioCtx = new AudioContext();
	return audioCtx;
}

// MIDI note number to frequency
function midiToFreq(note) {
	return 440 * Math.pow(2, (note - 69) / 12);
}

// Note name (e.g. "C4", "F#5") to MIDI number
function nameToMidi(name) {
	const notes = { C:0, D:2, E:4, F:5, G:7, A:9, B:11 };
	const match = name.match(/^([A-G])(#|b)?(\d+)$/i);
	if (!match) return 60;
	let [, letter, accidental, octave] = match;
	let midi = notes[letter.toUpperCase()] + (parseInt(octave) + 1) * 12;
	if (accidental === '#') midi++;
	if (accidental === 'b') midi--;
	return midi;
}

function beatsToSeconds(beats) {
	return (beats / tempo) * 60;
}

// Track implementations — each returns { result, sigils? }
const trackHandlers = {

	// === CANVAS ===

	'Create Canvas': (args) => {
		canvasWidth = args[0] || 400;
		canvasHeight = args[1] || 300;
		const container = document.getElementById('canvas-container');
		if (container) {
			container.innerHTML = '';
			canvas = document.createElement('canvas');
			canvas.width = canvasWidth;
			canvas.height = canvasHeight;
			canvas.style.background = '#111';
			canvas.style.borderRadius = '4px';
			container.appendChild(canvas);
			ctx = canvas.getContext('2d');
			ctx.fillStyle = '#fff';
			ctx.strokeStyle = '#fff';
			ctx.lineWidth = 2;
			ctx.font = '16px monospace';
		}
		return { result: null, sigils: { '2': canvasWidth * 10000 + canvasHeight } };
	},

	'Set Color': (args) => {
		if (ctx) ctx.fillStyle = args[0] || '#fff';
		return { result: null };
	},

	'Set Stroke': (args) => {
		if (ctx) ctx.strokeStyle = args[0] || '#fff';
		return { result: null };
	},

	'Set Line Width': (args) => {
		if (ctx) ctx.lineWidth = args[0] || 1;
		return { result: null };
	},

	'Clear Canvas': () => {
		if (ctx) ctx.clearRect(0, 0, canvasWidth, canvasHeight);
		return { result: null };
	},

	'Draw Rectangle': (args) => {
		if (ctx) ctx.fillRect(args[0], args[1], args[2], args[3]);
		return { result: null };
	},

	'Draw Outline': (args) => {
		if (ctx) ctx.strokeRect(args[0], args[1], args[2], args[3]);
		return { result: null };
	},

	'Draw Circle': (args) => {
		if (ctx) {
			ctx.beginPath();
			ctx.arc(args[0], args[1], args[2], 0, Math.PI * 2);
			ctx.fill();
		}
		return { result: null };
	},

	'Draw Line': (args) => {
		if (ctx) {
			ctx.beginPath();
			ctx.moveTo(args[0], args[1]);
			ctx.lineTo(args[2], args[3]);
			ctx.stroke();
		}
		return { result: null };
	},

	'Write Text': (args) => {
		if (ctx) ctx.fillText(args[0] || '', args[1] || 0, args[2] || 0);
		return { result: null };
	},

	'Set Font': (args) => {
		if (ctx) ctx.font = args[0] || '16px monospace';
		return { result: null };
	},

	'Canvas Width': () => ({ result: canvasWidth }),
	'Canvas Height': () => ({ result: canvasHeight }),

	// === AUDIO ===

	'Play Note': (args) => {
		const ac = ensureAudio();
		const freq = midiToFreq(args[0] || 60);
		const duration = beatsToSeconds(args[1] || 1);
		const osc = ac.createOscillator();
		const gain = ac.createGain();
		osc.type = instrument;
		osc.frequency.value = freq;
		gain.gain.value = volume;
		gain.gain.exponentialRampToValueAtTime(0.001, ac.currentTime + duration);
		osc.connect(gain);
		gain.connect(ac.destination);
		osc.start();
		osc.stop(ac.currentTime + duration);
		return { result: null };
	},

	'Play Chord': (args) => {
		const ac = ensureAudio();
		const chordName = (args[0] || 'C4').toString();
		const duration = beatsToSeconds(args[1] || 1);
		// Parse chord: "C4" = single note, "C4,E4,G4" = multiple
		const noteNames = chordName.split(',').map(s => s.trim());
		for (const name of noteNames) {
			const freq = midiToFreq(nameToMidi(name));
			const osc = ac.createOscillator();
			const gain = ac.createGain();
			osc.type = instrument;
			osc.frequency.value = freq;
			gain.gain.value = volume / noteNames.length;
			gain.gain.exponentialRampToValueAtTime(0.001, ac.currentTime + duration);
			osc.connect(gain);
			gain.connect(ac.destination);
			osc.start();
			osc.stop(ac.currentTime + duration);
		}
		return { result: null };
	},

	'Set Volume': (args) => {
		volume = Math.max(0, Math.min(1, (args[0] || 50) / 100));
		return { result: null };
	},

	'Set Tempo': (args) => {
		tempo = args[0] || 120;
		return { result: null };
	},

	'Set Instrument': (args) => {
		const map = { sine: 'sine', square: 'square', sawtooth: 'sawtooth', triangle: 'triangle',
			guitar: 'sawtooth', piano: 'triangle', bass: 'sine', synth: 'square' };
		instrument = map[(args[0] || 'sine').toLowerCase()] || 'sine';
		return { result: null };
	},

	'Rest': (args) => {
		// Silence for N beats — handled synchronously (no actual delay in WASM)
		return { result: null };
	},

	'Play Frequency': (args) => {
		const ac = ensureAudio();
		const freq = args[0] || 440;
		const duration = beatsToSeconds(args[1] || 1);
		const osc = ac.createOscillator();
		const gain = ac.createGain();
		osc.type = instrument;
		osc.frequency.value = freq;
		gain.gain.value = volume;
		gain.gain.exponentialRampToValueAtTime(0.001, ac.currentTime + duration);
		osc.connect(gain);
		gain.connect(ac.destination);
		osc.start();
		osc.stop(ac.currentTime + duration);
		return { result: null };
	},
};

// Tracklist content served via module resolver
const builtinTracklists = {};

export async function loadTracklists() {
	try {
		const [canvasResp, audioResp] = await Promise.all([
			fetch('/tracklists/canvas.tracklist'),
			fetch('/tracklists/audio.tracklist'),
		]);
		builtinTracklists['canvas.tracklist'] = await canvasResp.text();
		builtinTracklists['audio.tracklist'] = await audioResp.text();
	} catch (e) {
		console.warn('Could not load built-in tracklists:', e);
	}
}

export function resolveTracklist(path) {
	return builtinTracklists[path] || null;
}

export function handleTrackCall(trackName, argsJson) {
	const args = JSON.parse(argsJson);
	const handler = trackHandlers[trackName];
	if (!handler) {
		return JSON.stringify({ result: null });
	}
	const result = handler(args);
	return JSON.stringify(result);
}
