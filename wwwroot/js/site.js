(() => {
  const toggle = document.querySelector("[data-menu-toggle]");
  const navigation = document.querySelector("[data-primary-nav]");

  if (toggle && navigation) {
    const closeMenu = () => {
      toggle.setAttribute("aria-expanded", "false");
      navigation.classList.remove("is-open");
    };

    toggle.addEventListener("click", () => {
      const isOpen = toggle.getAttribute("aria-expanded") === "true";
      toggle.setAttribute("aria-expanded", String(!isOpen));
      navigation.classList.toggle("is-open", !isOpen);
    });

    navigation.addEventListener("click", (event) => {
      if (event.target.closest("a")) closeMenu();
    });

    document.addEventListener("keydown", (event) => {
      if (event.key === "Escape") closeMenu();
    });
  }

  const hero = document.querySelector("[data-hero-visual]");
  if (!hero) return;

  const reducedMotion = window.matchMedia("(prefers-reduced-motion: reduce)");
  const finePointer = window.matchMedia("(hover: hover) and (pointer: fine)");
  const ring = hero.querySelector("[data-progress-ring]");
  const progressNumber = hero.querySelector("[data-progress-number]");
  const routeSteps = [...hero.querySelectorAll("[data-route-step]")];
  const note = hero.querySelector("[data-dashboard-note]");
  const targetProgress = Number(ring?.dataset.progressValue ?? 64);

  if (!reducedMotion.matches && ring && progressNumber && routeSteps.length && note) {
    hero.classList.add("is-sequenced");
    ring.style.setProperty("--hero-progress", "0%");
    progressNumber.textContent = "0";

    let frameId = 0;
    let startedAt = 0;
    let hiddenAt = 0;
    let started = false;

    const revealTimeline = (now) => {
      const elapsed = now - startedAt;
      const progress = Math.min(1, Math.max(0, (elapsed - 180) / 850));
      const easedProgress = 1 - Math.pow(1 - progress, 3);
      const displayedProgress = Math.round(targetProgress * easedProgress);

      ring.style.setProperty("--hero-progress", `${targetProgress * easedProgress}%`);
      progressNumber.textContent = String(displayedProgress);

      if (elapsed >= 80) routeSteps[0]?.classList.add("is-revealed");
      if (elapsed >= 430) routeSteps[1]?.classList.add("is-revealed");
      if (elapsed >= 760) routeSteps[2]?.classList.add("is-revealed");
      if (elapsed >= 1120) note.classList.add("is-revealed");

      if (elapsed < 1580) frameId = window.requestAnimationFrame(revealTimeline);
    };

    const startTimeline = () => {
      if (started || document.hidden) return;
      started = true;
      startedAt = performance.now();
      frameId = window.requestAnimationFrame(revealTimeline);
    };

    document.addEventListener("visibilitychange", () => {
      if (document.hidden) {
        hiddenAt = performance.now();
        window.cancelAnimationFrame(frameId);
        return;
      }

      if (!started) {
        startTimeline();
        return;
      }

      if (hiddenAt) startedAt += performance.now() - hiddenAt;
      frameId = window.requestAnimationFrame(revealTimeline);
    });

    startTimeline();
  }

  if (!reducedMotion.matches && finePointer.matches) {
    const chips = [...hero.querySelectorAll("[data-parallax-chip]")];
    let parallaxFrame = 0;
    let pointerX = 0;
    let pointerY = 0;

    const renderParallax = () => {
      chips.forEach((chip) => {
        const direction = chip.dataset.parallaxChip === "reverse" ? -1 : 1;
        chip.style.setProperty("--chip-x", `${pointerX * 8 * direction}px`);
        chip.style.setProperty("--chip-y", `${pointerY * 6 * direction}px`);
      });
      parallaxFrame = 0;
    };

    hero.addEventListener("pointermove", (event) => {
      if (document.hidden) return;
      const bounds = hero.getBoundingClientRect();
      pointerX = ((event.clientX - bounds.left) / bounds.width - .5) * 2;
      pointerY = ((event.clientY - bounds.top) / bounds.height - .5) * 2;
      if (!parallaxFrame) parallaxFrame = window.requestAnimationFrame(renderParallax);
    });

    hero.addEventListener("pointerleave", () => {
      pointerX = 0;
      pointerY = 0;
      if (!parallaxFrame) parallaxFrame = window.requestAnimationFrame(renderParallax);
    });
  }
})();
