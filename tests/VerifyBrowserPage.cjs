const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');
const assert = require('node:assert/strict');
const html = fs.readFileSync(path.join(__dirname, '..', 'BrowserSource.html'), 'utf8');
const script = html.match(/<script>([\s\S]*?)<\/script>/)[1].replace('void connect();', '');
const elements = {
  stage: { style: {} },
  announcement: { style: {}, removeAttribute(name) { delete this[name]; } }
};
const timers = new Map(); let timerId = 0, audioPlays = 0, audioStops = 0, pendingDecode = null;
class TestAudio { constructor() { this.listeners = {}; } addEventListener(name, callback) { this.listeners[name] = callback; } async play() { audioPlays++; } pause() { audioStops++; } }
class TestImage { async decode() { if (pendingDecode) await pendingDecode; } }
const context = vm.createContext({
  document: { getElementById: id => elements[id] }, window: { addEventListener() {} },
  Audio: TestAudio, Image: TestImage, URL: { createObjectURL: () => 'blob:test', revokeObjectURL() {} },
  fetch: async () => ({ ok: true, blob: async () => ({}) }), console: { warn() {}, error() {} },
  setTimeout: (callback, ms) => { timers.set(++timerId, { callback, ms }); return timerId; }, clearTimeout: id => timers.delete(id)
});
vm.runInContext(script, context);
async function play(event) { context.testEvent = event; await vm.runInContext('play(testEvent)', context); }
(async () => {
  await play({ visualUrl: 'image/first', url: 'audio/first', preview: true, volume: .34, position: 'TopLeft', durationMs: 5500 });
  assert.equal(audioPlays, 1); assert.equal(elements.announcement.style.display, 'block');
  assert.equal(elements.stage.style.justifyContent, undefined); assert.equal(elements.stage.style.alignItems, undefined);
  assert(!html.includes('position:fixed')); assert(!html.includes('padding:16px'));
  assert.equal([...timers.values()][0].ms, 5500);
  vm.runInContext('stopPreview()', context);
  assert.equal(elements.announcement.style.display, 'none'); assert.equal(audioStops, 1);
  await play({ visualUrl: 'image/normal', url: null, preview: false, position: 'BottomRight', durationMs: 3000 });
  assert.equal(audioPlays, 1); assert.equal(elements.announcement.src, 'image/normal');
  vm.runInContext('stopPreview()', context); assert.equal(elements.announcement.style.display, 'block');
  [...timers.values()][0].callback(); assert.equal(elements.announcement.style.display, 'none');
  let release; pendingDecode = new Promise(resolve => { release = resolve; });
  const late = play({ visualUrl: 'image/late', url: 'audio/late', preview: true, position: 'Center', durationMs: 3000 });
  vm.runInContext('stopPreview()', context); release(); await late; pendingDecode = null;
  assert.equal(elements.announcement.style.display, 'none'); assert.equal(audioPlays, 1);
  await play({ visualUrl: 'image/previous', url: null, preview: false, position: 'Center', durationMs: 3000 });
  await play({ visualUrl: null, url: 'audio/only', preview: false, volume: .5 });
  assert.equal(elements.announcement.style.display, 'none'); assert.equal(audioPlays, 2);
  process.stdout.write('PASS: synchronized browser audio/image, position, timed hide, preview stop, visual-only playback, late-load cancellation, audio-only clearing\n');
})().catch(error => { process.stderr.write(String(error.stack)); process.exitCode = 1; });
