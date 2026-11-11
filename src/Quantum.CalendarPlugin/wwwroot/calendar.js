const initializedDialogs = new WeakSet();
const initializedWeeks = new WeakMap();

export function syncWeekViewport(viewport, week, minute, selectedDay = 0) {
  if (!viewport?.isConnected) {
    return;
  }

  const previous = initializedWeeks.get(viewport);
  if (previous?.week === week && previous?.selectedDay === selectedDay) {
    return;
  }

  const timeline = viewport.querySelector('[data-week-timeline]');
  if (!timeline) {
    return;
  }

  if (previous?.week !== week) {
    viewport.scrollTop = (minute / 1440) * timeline.clientHeight;
  }

  const selected = viewport.querySelectorAll('.week-date')[selectedDay];
  if (selected && viewport.scrollWidth > viewport.clientWidth) {
    const bounds = viewport.getBoundingClientRect();
    const day = selected.getBoundingClientRect();
    if (day.left < bounds.left + 48) {
      viewport.scrollLeft += day.left - bounds.left - 48;
    } else if (day.right > bounds.left + viewport.clientWidth) {
      viewport.scrollLeft += day.right - bounds.left - viewport.clientWidth;
    }
  }

  initializedWeeks.set(viewport, { week, selectedDay });
}

export function syncEditor(dialog, open) {
  if (!dialog?.isConnected) {
    return;
  }

  if (!initializedDialogs.has(dialog)) {
    dialog.addEventListener('cancel', (event) => {
      event.preventDefault();
      dialog.querySelector('[data-editor-close]')?.click();
    });
    dialog.addEventListener(
      'invalid',
      (event) => {
        event.target.closest('details')?.setAttribute('open', '');
      },
      true,
    );
    initializedDialogs.add(dialog);
  }

  if (open && !dialog.open) {
    dialog.showModal();
  } else if (!open && dialog.open) {
    dialog.close();
  }
}
