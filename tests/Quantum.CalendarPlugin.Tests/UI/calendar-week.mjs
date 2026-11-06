import assert from 'node:assert/strict';
import test from 'node:test';
import { syncWeekViewport } from '../../../src/Quantum.CalendarPlugin/wwwroot/calendar.js';

test('week viewport starts at the chosen hour and retains manual scrolling within a week', () => {
  const viewport = {
    isConnected: true,
    scrollTop: 0,
    querySelectorAll: () => [],
    querySelector: () => ({ clientHeight: 1536 }),
  };
  syncWeekViewport(viewport, 100, 480);
  assert.equal(viewport.scrollTop, 512);
  viewport.scrollTop = 700;
  syncWeekViewport(viewport, 100, 360);
  assert.equal(viewport.scrollTop, 700);
  syncWeekViewport(viewport, 107, 360);
  assert.equal(viewport.scrollTop, 384);
});

test('detached or incomplete week views do not consume their initial scroll', () => {
  syncWeekViewport(null, 100, 480);
  const viewport = {
    isConnected: false,
    scrollTop: 0,
    querySelectorAll: () => [],
    querySelector: () => null,
  };
  syncWeekViewport(viewport, 100, 480);
  viewport.isConnected = true;
  syncWeekViewport(viewport, 100, 480);
  assert.equal(viewport.scrollTop, 0);
  viewport.querySelector = () => ({ clientHeight: 1536 });
  syncWeekViewport(viewport, 100, 480);
  assert.equal(viewport.scrollTop, 512);
});

test('a narrow viewport reveals the selected day without resetting vertical scrolling', () => {
  const viewport = {
    isConnected: true,
    clientWidth: 300,
    scrollWidth: 748,
    scrollTop: 0,
    scrollLeft: 0,
    querySelector: () => ({ clientHeight: 1536 }),
    querySelectorAll: () => [
      null,
      null,
      null,
      { getBoundingClientRect: () => ({ left: 350, right: 450 }) },
    ],
    getBoundingClientRect: () => ({ left: 0 }),
  };
  syncWeekViewport(viewport, 100, 480, 0);
  viewport.scrollTop = 700;
  syncWeekViewport(viewport, 100, 480, 3);
  assert.equal(viewport.scrollTop, 700);
  assert.equal(viewport.scrollLeft, 150);
});
