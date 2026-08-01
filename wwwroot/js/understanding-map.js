(() => {
  const board = document.querySelector("[data-map-lane]")?.closest(".map-section");
  if (!board) return;

  const nodes = [...board.querySelectorAll("[data-map-node]")];
  const lanes = [...board.querySelectorAll("[data-map-lane]")];
  const mapLanes = board.querySelector(".map-lanes");
  const gradeFilters = [...board.querySelectorAll("[data-grade-filter]")];
  const areaFilters = [...board.querySelectorAll("[data-area-filter]")];
  const detail = board.querySelector("[data-detail-content]");
  const visibleCount = board.querySelector("[data-visible-count]");
  let selectedGrade = "all";
  let selectedArea = "all";

  const renderDetail = (node, shouldScroll = false) => {
    const template = document.querySelector(`#detail-${node.dataset.mapNode}`);
    if (!template || !detail) return;

    nodes.forEach((item) => {
      const selected = item === node;
      item.classList.toggle("is-selected", selected);
      item.setAttribute("aria-pressed", String(selected));
    });
    detail.replaceChildren(template.content.cloneNode(true));

    if (shouldScroll && window.matchMedia("(max-width: 760px)").matches) {
      document.querySelector("#map-detail")?.scrollIntoView({ behavior: "smooth", block: "start" });
    }
  };

  const updateFilters = () => {
    let count = 0;

    lanes.forEach((lane) => {
      const areaMatches = selectedArea === "all" || lane.dataset.mapLane === selectedArea;
      lane.hidden = !areaMatches;

      lane.querySelectorAll("[data-grade-group]").forEach((group) => {
        const gradeMatches = selectedGrade === "all" || group.dataset.gradeGroup === selectedGrade;
        group.hidden = !gradeMatches;
        if (areaMatches && gradeMatches) count += group.querySelectorAll("[data-map-node]").length;
      });
    });

    if (visibleCount) visibleCount.textContent = String(count);
    mapLanes?.classList.toggle("is-single-area", selectedArea !== "all");

    const selectedNode = nodes.find((node) => node.classList.contains("is-selected"));
    const selectedIsVisible = selectedNode && !selectedNode.closest("[hidden]");
    if (!selectedIsVisible) {
      const firstVisible = nodes.find((node) => !node.closest("[hidden]"));
      if (firstVisible) renderDetail(firstVisible);
    }
  };

  const activateFilter = (buttons, activeButton) => {
    buttons.forEach((button) => {
      const active = button === activeButton;
      button.classList.toggle("is-active", active);
      button.setAttribute("aria-pressed", String(active));
    });
  };

  nodes.forEach((node) => node.addEventListener("click", () => renderDetail(node, true)));

  gradeFilters.forEach((button) => button.addEventListener("click", () => {
    selectedGrade = button.dataset.gradeFilter;
    activateFilter(gradeFilters, button);
    updateFilters();
  }));

  areaFilters.forEach((button) => button.addEventListener("click", () => {
    selectedArea = button.dataset.areaFilter;
    activateFilter(areaFilters, button);
    updateFilters();
  }));

  const initialNode = nodes.find((node) => node.classList.contains("is-selected")) ?? nodes[0];
  if (initialNode) renderDetail(initialNode);
})();
