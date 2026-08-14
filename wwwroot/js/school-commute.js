(() => {
  const settingsKey = "wakaroute.commute-settings.v1";
  const recordsKey = "wakaroute.commute-records.v1";
  const page = document.querySelector("[data-school-commute-page]");
  if (!page) return;

  const readObject = (key) => {
    try {
      const value = JSON.parse(window.localStorage.getItem(key) ?? "{}");
      return value && !Array.isArray(value) && typeof value === "object" ? value : {};
    } catch {
      return {};
    }
  };
  const writeObject = (key, value) => {
    try {
      window.localStorage.setItem(key, JSON.stringify(value));
      return true;
    } catch {
      return false;
    }
  };
  const numericValue = (input) => input?.value === "" ? null : Number(input?.value);
  const safeNumber = (value, minimum, maximum) => Number.isFinite(value) ? Math.min(maximum, Math.max(minimum, value)) : null;

  let settings = readObject(settingsKey);
  let records = readObject(recordsKey);
  const origin = page.querySelector("[data-commute-origin]");
  const maxMinutes = page.querySelector("[data-commute-max-minutes]");
  const maxTransfers = page.querySelector("[data-commute-max-transfers]");
  const filterStatus = page.querySelector("[data-commute-filter-status]");
  const cards = [...page.querySelectorAll("[data-commute-school]")];

  if (origin) origin.value = typeof settings.origin === "string" ? settings.origin : "";
  if (maxMinutes) maxMinutes.value = typeof settings.maxMinutes === "string" ? settings.maxMinutes : "";
  if (maxTransfers) maxTransfers.value = typeof settings.maxTransfers === "string" ? settings.maxTransfers : "";

  const fieldsFor = (card) => ({
    minutes: card.querySelector("[data-commute-minutes]"),
    transfers: card.querySelector("[data-commute-transfers]"),
    walk: card.querySelector("[data-commute-walk]"),
    fare: card.querySelector("[data-commute-fare]"),
    note: card.querySelector("[data-commute-note]")
  });

  const updateDirections = () => {
    const departure = origin?.value.trim() ?? "";
    cards.forEach((card) => {
      const link = card.querySelector("[data-commute-directions]");
      if (!link) return;
      if (!departure) {
        link.href = "#commute-settings-heading";
        link.setAttribute("aria-disabled", "true");
        link.textContent = "先に出発地を入力";
        return;
      }
      const parameters = new URLSearchParams({
        api: "1",
        origin: departure,
        destination: card.dataset.schoolDestination ?? "",
        travelmode: "transit"
      });
      link.href = `https://www.google.com/maps/dir/?${parameters}`;
      link.removeAttribute("aria-disabled");
      link.textContent = "出発地から経路を調べる ↗";
    });
  };

  const render = () => {
    const minuteLimit = numericValue(maxMinutes);
    const transferLimit = numericValue(maxTransfers);
    let matched = 0;
    let excluded = 0;
    let unknown = 0;

    cards.forEach((card) => {
      const schoolId = card.dataset.schoolId;
      const record = records[schoolId] ?? {};
      const fields = fieldsFor(card);
      fields.minutes.value = Number.isFinite(record.minutes) ? String(record.minutes) : "";
      fields.transfers.value = Number.isFinite(record.transfers) ? String(record.transfers) : "";
      fields.walk.value = Number.isFinite(record.walk) ? String(record.walk) : "";
      fields.fare.value = Number.isFinite(record.fare) ? String(record.fare) : "";
      fields.note.value = typeof record.note === "string" ? record.note : "";

      const hasTime = Number.isFinite(record.minutes);
      const outside = hasTime && ((minuteLimit !== null && record.minutes > minuteLimit) ||
        (transferLimit !== null && Number.isFinite(record.transfers) && record.transfers > transferLimit));
      card.hidden = outside;
      card.dataset.commuteMatch = hasTime ? (outside ? "outside" : "inside") : "unknown";
      if (!hasTime) unknown += 1;
      else if (outside) excluded += 1;
      else matched += 1;

      const result = card.querySelector("[data-commute-result]");
      if (result) {
        const strong = result.querySelector("strong");
        const detail = result.querySelector("span");
        strong.textContent = hasTime ? `片道 ${record.minutes}分` : "未確認";
        detail.textContent = hasTime
          ? `${Number.isFinite(record.transfers) ? `乗換${record.transfers}回` : "乗換未入力"}${Number.isFinite(record.fare) ? `・${record.fare}円` : ""}`
          : "経路を調べて記録";
      }
      const savedStatus = card.querySelector("[data-commute-saved-status]");
      if (savedStatus) savedStatus.textContent = record.checkedAt ? `${record.checkedAt} にこの端末へ保存` : "まだ記録していません";
    });

    if (filterStatus) {
      const hasLimit = minuteLimit !== null || transferLimit !== null;
      filterStatus.textContent = hasLimit
        ? `条件内 ${matched}校・条件外 ${excluded}校・未確認 ${unknown}校。未確認の学校は候補に残しています。`
        : `記録済み ${matched}校・未確認 ${unknown}校です。`;
    }
    updateDirections();
  };

  const saveSettings = () => {
    settings = {
      origin: origin?.value.trim().slice(0, 80) ?? "",
      maxMinutes: maxMinutes?.value ?? "",
      maxTransfers: maxTransfers?.value ?? ""
    };
    writeObject(settingsKey, settings);
    render();
  };

  [origin, maxMinutes, maxTransfers].forEach((control) => control?.addEventListener("input", saveSettings));
  [maxMinutes, maxTransfers].forEach((control) => control?.addEventListener("change", saveSettings));

  cards.forEach((card) => {
    const schoolId = card.dataset.schoolId;
    const fields = fieldsFor(card);
    const saveRecord = () => {
      const minutes = safeNumber(numericValue(fields.minutes), 1, 300);
      const transfers = safeNumber(numericValue(fields.transfers), 0, 9);
      const walk = safeNumber(numericValue(fields.walk), 0, 120);
      const fare = safeNumber(numericValue(fields.fare), 0, 10000);
      const note = fields.note.value.trim().slice(0, 120);
      if (minutes === null && transfers === null && walk === null && fare === null && !note) delete records[schoolId];
      else records[schoolId] = { minutes, transfers, walk, fare, note, checkedAt: new Date().toLocaleDateString("ja-JP") };
      writeObject(recordsKey, records);
      render();
    };
    Object.values(fields).forEach((field) => field.addEventListener("input", saveRecord));
    card.querySelector("[data-commute-clear]")?.addEventListener("click", () => {
      delete records[schoolId];
      writeObject(recordsKey, records);
      render();
    });
    card.querySelector("[data-commute-directions]")?.addEventListener("click", (event) => {
      if (origin?.value.trim()) return;
      event.preventDefault();
      origin?.focus();
      if (filterStatus) filterStatus.textContent = "経路を調べる前に、最寄り駅または大まかな出発地を入力してください。";
    });
  });

  render();
})();
