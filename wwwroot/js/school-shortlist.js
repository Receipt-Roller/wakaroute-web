(() => {
  const favoritesKey = "wakaroute.school-favorites.v1";
  const comparisonKey = "wakaroute.school-comparison.v1";
  const notesKey = "wakaroute.school-notes.v1";
  const maximumComparison = 3;
  const schoolIdPattern = /^wk_[a-z0-9_]+$/;

  const readJson = (key, fallback) => {
    try {
      const value = JSON.parse(window.localStorage.getItem(key) ?? "null");
      return value ?? fallback;
    } catch {
      return fallback;
    }
  };

  const writeJson = (key, value) => {
    try {
      window.localStorage.setItem(key, JSON.stringify(value));
      return true;
    } catch {
      return false;
    }
  };

  const uniqueIds = (values) => [...new Set(values.filter((value) => typeof value === "string" && schoolIdPattern.test(value)))];
  const storedFavorites = readJson(favoritesKey, []);
  const storedComparison = readJson(comparisonKey, []);
  let favorites = uniqueIds(Array.isArray(storedFavorites) ? storedFavorites : []);
  let comparison = (Array.isArray(storedComparison) ? storedComparison : [])
    .map((item) => typeof item === "string" ? { id: item, name: item } : item)
    .filter((item) => item && schoolIdPattern.test(item.id))
    .filter((item, index, items) => items.findIndex((candidate) => candidate.id === item.id) === index)
    .slice(0, maximumComparison);
  let notes = readJson(notesKey, {});
  if (!notes || Array.isArray(notes) || typeof notes !== "object") notes = {};

  const status = document.querySelector("[data-school-shortlist-status]");
  const announce = (message) => {
    if (!status) return;
    status.textContent = "";
    window.requestAnimationFrame(() => { status.textContent = message; });
  };

  const buttonSchool = (button) => ({
    id: button.dataset.schoolId ?? "",
    name: button.dataset.schoolName?.trim() || button.dataset.schoolId || "高校"
  });

  const updateControls = (root = document) => {
    root.querySelectorAll("[data-school-favorite]").forEach((button) => {
      const selected = favorites.includes(button.dataset.schoolId ?? "");
      button.setAttribute("aria-pressed", String(selected));
      const label = button.querySelector("[data-school-favorite-label]");
      if (label) label.textContent = selected ? "お気に入り済み" : "お気に入り";
    });

    root.querySelectorAll("[data-school-compare]").forEach((button) => {
      const selected = comparison.some((item) => item.id === button.dataset.schoolId);
      button.setAttribute("aria-pressed", String(selected));
      const label = button.querySelector("[data-school-compare-label]");
      if (label) label.textContent = selected ? "比較から外す" : "比較に追加";
    });

    document.querySelectorAll("[data-school-favorite-count]").forEach((element) => {
      element.textContent = String(favorites.length);
    });

    document.querySelectorAll("[data-school-commute-link]").forEach((link) => {
      const ids = encodeURIComponent(favorites.slice(0, 5).join(","));
      link.href = ids ? `/schools/commute?ids=${ids}` : "/schools/commute";
    });
  };

  const renderTray = () => {
    const tray = document.querySelector("[data-school-compare-tray]");
    if (!tray) return;
    tray.hidden = comparison.length === 0;
    const count = tray.querySelector("[data-school-compare-count]");
    if (count) count.textContent = String(comparison.length);
    const list = tray.querySelector("[data-school-compare-items]");
    if (list) {
      list.replaceChildren();
      comparison.forEach((school) => {
        const item = document.createElement("li");
        const name = document.createElement("span");
        name.textContent = school.name;
        const remove = document.createElement("button");
        remove.type = "button";
        remove.dataset.compareRemove = school.id;
        remove.setAttribute("aria-label", `${school.name}を比較から外す`);
        remove.textContent = "×";
        item.append(name, remove);
        list.append(item);
      });
    }
    const link = tray.querySelector("[data-school-compare-link]");
    if (link) link.href = `/schools/compare?ids=${encodeURIComponent(comparison.map((item) => item.id).join(","))}`;
  };

  const persistFavorites = () => {
    if (!writeJson(favoritesKey, favorites)) announce("このブラウザではお気に入りを保存できませんでした。");
  };
  const persistComparison = () => {
    if (!writeJson(comparisonKey, comparison)) announce("このブラウザでは比較候補を保存できませんでした。");
  };

  const removeSavedCard = (schoolId) => {
    const card = document.querySelector(`[data-saved-school-card][data-school-id="${CSS.escape(schoolId)}"]`);
    card?.remove();
    const savedPage = document.querySelector("[data-school-saved-page]");
    if (!savedPage) return;
    const empty = savedPage.querySelector("[data-saved-empty]");
    const list = savedPage.querySelector("[data-saved-list]");
    if (empty && list) empty.hidden = list.children.length > 0;
  };

  document.addEventListener("click", (event) => {
    if (!(event.target instanceof Element)) return;
    const favoriteButton = event.target.closest("[data-school-favorite]");
    if (favoriteButton) {
      const school = buttonSchool(favoriteButton);
      if (!schoolIdPattern.test(school.id)) return;
      if (favorites.includes(school.id)) {
        favorites = favorites.filter((id) => id !== school.id);
        removeSavedCard(school.id);
        announce(`${school.name}をお気に入りから外しました。`);
      } else {
        favorites = [...favorites, school.id];
        announce(`${school.name}をお気に入りに保存しました。`);
      }
      persistFavorites();
      updateControls();
      return;
    }

    const compareButton = event.target.closest("[data-school-compare]");
    if (compareButton) {
      const school = buttonSchool(compareButton);
      if (!schoolIdPattern.test(school.id)) return;
      const wasSelected = comparison.some((item) => item.id === school.id);
      if (wasSelected) {
        comparison = comparison.filter((item) => item.id !== school.id);
        announce(`${school.name}を比較から外しました。`);
      } else if (comparison.length >= maximumComparison) {
        announce("比較できるのは最大3校です。1校外してから追加してください。");
        return;
      } else {
        comparison = [...comparison, school];
        announce(`${school.name}を比較に追加しました。`);
      }
      persistComparison();
      updateControls();
      renderTray();
      if (wasSelected && document.querySelector("[data-school-comparison-page]")) {
        const ids = encodeURIComponent(comparison.map((item) => item.id).join(","));
        window.location.assign(ids ? `/schools/compare?ids=${ids}` : "/schools/compare");
      }
      return;
    }

    const removeButton = event.target.closest("[data-compare-remove]");
    if (removeButton) {
      const removed = comparison.find((item) => item.id === removeButton.dataset.compareRemove);
      comparison = comparison.filter((item) => item.id !== removeButton.dataset.compareRemove);
      persistComparison();
      updateControls();
      renderTray();
      if (removed) announce(`${removed.name}を比較から外しました。`);
    }
  });

  const initializeComparisonPage = () => {
    const page = document.querySelector("[data-school-comparison-page]");
    if (!page) return;
    const ids = uniqueIds((page.dataset.comparisonIds ?? "").split(",")).slice(0, maximumComparison);
    if (ids.length === 0) return;
    comparison = ids.map((id) => {
      const button = document.querySelector(`[data-school-compare][data-school-id="${CSS.escape(id)}"]`);
      return { id, name: button?.dataset.schoolName?.trim() || id };
    });
    persistComparison();
  };

  const initializeNotes = () => {
    document.querySelectorAll("[data-school-note]").forEach((textarea) => {
      const schoolId = textarea.dataset.schoolId;
      if (!schoolIdPattern.test(schoolId ?? "")) return;
      textarea.value = typeof notes[schoolId] === "string" ? notes[schoolId] : "";
      const noteStatus = textarea.parentElement?.querySelector("[data-school-note-status]");
      if (noteStatus) noteStatus.textContent = textarea.value ? "この端末に保存済み" : "未入力";
      textarea.addEventListener("input", () => {
        const value = textarea.value.slice(0, 500);
        if (value) notes[schoolId] = value;
        else delete notes[schoolId];
        const saved = writeJson(notesKey, notes);
        if (noteStatus) noteStatus.textContent = saved ? (value ? "この端末に保存済み" : "未入力") : "保存できませんでした";
      });
    });
  };

  const createSavedCard = (details, template) => {
    const school = details.school;
    const fragment = template.content.cloneNode(true);
    const card = fragment.querySelector("[data-saved-school-card]");
    card.dataset.schoolId = school.id;
    const ownership = card.querySelector("[data-saved-ownership]");
    ownership.textContent = school.ownershipLabel;
    ownership.classList.add(`school-tag-${school.ownership}`);
    card.querySelector("[data-saved-gender]").textContent = school.genderLabel;
    const name = card.querySelector("[data-saved-name]");
    name.textContent = school.name;
    name.href = `/schools/${encodeURIComponent(school.id)}`;
    card.querySelector("[data-saved-program]").textContent = school.programSummary;
    card.querySelector("[data-saved-address]").textContent = school.address;
    card.querySelector("[data-saved-verified]").textContent = `${school.lastVerifiedAt} 確認`;
    card.querySelector("[data-saved-details]").href = `/schools/${encodeURIComponent(school.id)}`;
    card.querySelectorAll("[data-school-favorite], [data-school-compare]").forEach((button) => {
      button.dataset.schoolId = school.id;
      button.dataset.schoolName = school.name;
    });
    return fragment;
  };

  const initializeSavedPage = async () => {
    const page = document.querySelector("[data-school-saved-page]");
    if (!page) return;
    const loading = page.querySelector("[data-saved-loading]");
    const empty = page.querySelector("[data-saved-empty]");
    const list = page.querySelector("[data-saved-list]");
    const template = page.querySelector("[data-saved-school-template]");
    if (!list || !template) return;

    if (favorites.length === 0) {
      if (loading) loading.hidden = true;
      if (empty) empty.hidden = false;
      return;
    }

    const results = await Promise.allSettled(favorites.map(async (id) => {
      const response = await window.fetch(`/api/schools/${encodeURIComponent(id)}`, { headers: { Accept: "application/json" } });
      if (!response.ok) throw new Error(`School ${id} was not found.`);
      return response.json();
    }));
    results.forEach((result) => {
      if (result.status === "fulfilled") list.append(createSavedCard(result.value, template));
    });
    if (loading) loading.hidden = true;
    if (empty) empty.hidden = list.children.length > 0;
    updateControls(list);
    if (results.some((result) => result.status === "rejected")) announce("一部の高校情報を読み込めませんでした。時間をおいて再度お試しください。");
  };

  initializeComparisonPage();
  initializeNotes();
  updateControls();
  renderTray();
  initializeSavedPage();

  window.addEventListener("storage", (event) => {
    if (![favoritesKey, comparisonKey].includes(event.key)) return;
    window.location.reload();
  });
})();
