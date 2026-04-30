import { dotnet } from '../wasm/wwwroot/_framework/dotnet.js'

const { getAssemblyExports, getConfig } = await dotnet.withDiagnosticTracing(false).create();
const config = getConfig();
const exports = await getAssemblyExports(config.mainAssemblyName);

var status = await exports.Rockstar.Wasm.RockstarRunner.Status();
self.postMessage({ type: 'ready', status: status });

// Module map populated per-run from the main thread
let currentModules = {};

// Pending shell commands (sync bridge via SharedArrayBuffer or polling)
let pendingShellResults = new Map();
let shellId = 0;

function resolveModule(path) {
	// Try exact match, then with .rock extension
	if (currentModules[path]) return currentModules[path];
	if (currentModules[path + '.rock']) return currentModules[path + '.rock'];
	// Try lowercase
	const lower = path.toLowerCase();
	for (const [key, value] of Object.entries(currentModules)) {
		if (key.toLowerCase() === lower || key.toLowerCase() === lower + '.rock') {
			return value;
		}
	}
	return null;
}

function executeCommand(cmd) {
	// For now, handle basic commands directly in the worker
	const trimmed = cmd.trim();

	if (trimmed.startsWith('echo ')) {
		return trimmed.slice(5);
	}
	if (trimmed === 'ls' || trimmed === 'dir') {
		return Object.keys(currentModules).join('\n');
	}
	if (trimmed.startsWith('cat ')) {
		const filename = trimmed.slice(4).trim();
		const content = currentModules[filename] || currentModules[filename + '.rock'];
		if (content) return content;
		return '';
	}
	if (trimmed === 'pwd') return '/playground';
	if (trimmed === 'date') return new Date().toISOString();

	return `sh: ${trimmed.split(' ')[0]}: command not found`;
}

function report(output) {
	self.postMessage({ type: 'output', output: output });
}

async function runProgram(source, modules) {
	currentModules = modules || {};
	try {
		var result = await exports.Rockstar.Wasm.RockstarRunner.RunWithModules(
			source,
			report,
			resolveModule,
			executeCommand
		);
		self.postMessage({ type: 'result', result: result });
	} catch (error) {
		self.postMessage({ type: 'error', error: error.toString() });
	}
}

async function parseProgram(source) {
	try {
		var result = await exports.Rockstar.Wasm.RockstarRunner.Parse(source);
		self.postMessage({ type: 'parse', result: result });
	} catch (error) {
		self.postMessage({ type: 'error', error: error.toString() });
	}
}

self.addEventListener('message', async function(message) {
	var data = message.data;
	switch (data.command) {
		case 'run':
			return await runProgram(data.source, data.modules);
		case 'parse':
			return await parseProgram(data.source);
	}
});
