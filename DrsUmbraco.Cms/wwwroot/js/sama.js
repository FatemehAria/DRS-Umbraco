(function () {
  "use strict";

  const SELECTORS = {
    heroSlider: "[data-slider]",
    heroSlide: ".hero-slide",
    heroDots: "[data-dots] button",
    heroNext: "[data-next]",
    heroPrev: "[data-prev]",

    revealSection: ".reveal-section",
    backToTop: "[data-back-to-top]",

    consultForm: "#consultRequestForm",
    consultMessage: "#consultRequestMessage",

    testimonials: "[data-testimonials]",
    testimonialSlide: ".testimonial-slide",
    testimonialDots: "[data-testimonial-dots] button",
    testimonialNext: "[data-testimonial-next]",
    testimonialPrev: "[data-testimonial-prev]",
  };

  const ACTIVE_CLASS = "is-active";
  const VISIBLE_CLASS = "is-visible";
  const SCROLLED_CLASS = "is-scrolled";

  document.addEventListener("DOMContentLoaded", function () {
    initHeroSlider();
    initRevealSections();
    initBackToTop();
    initConsultRequestForm();
    initTestimonialSlider();
    initHeaderScrollState();
    initSmoothAnchorLinks();
    initMobileMenu();
  });

  function initHeroSlider() {
    const slider = document.querySelector(SELECTORS.heroSlider);

    if (!slider) {
      return;
    }

    createSlider({
      root: slider,
      slideSelector: SELECTORS.heroSlide,
      dotSelector: SELECTORS.heroDots,
      nextSelector: SELECTORS.heroNext,
      prevSelector: SELECTORS.heroPrev,
      intervalMs: 3000,
    });
  }

  function initTestimonialSlider() {
    const testimonialRoot = document.querySelector(SELECTORS.testimonials);

    if (!testimonialRoot) {
      return;
    }

    createSlider({
      root: testimonialRoot,
      slideSelector: SELECTORS.testimonialSlide,
      dotSelector: SELECTORS.testimonialDots,
      nextSelector: SELECTORS.testimonialNext,
      prevSelector: SELECTORS.testimonialPrev,
      intervalMs: 5000,
    });
  }

  function createSlider({
    root,
    slideSelector,
    dotSelector,
    nextSelector,
    prevSelector,
    intervalMs,
  }) {
    const slides = Array.from(root.querySelectorAll(slideSelector));
    const dots = Array.from(root.querySelectorAll(dotSelector));
    const nextButton = root.querySelector(nextSelector);
    const prevButton = root.querySelector(prevSelector);

    if (slides.length === 0) {
      return;
    }

    let currentIndex = slides.findIndex((slide) =>
      slide.classList.contains(ACTIVE_CLASS),
    );

    if (currentIndex < 0) {
      currentIndex = 0;
    }

    let timerId = null;

    showSlide(currentIndex);
    startAutoPlay();

    nextButton?.addEventListener("click", function () {
      showSlide(currentIndex + 1);
      restartAutoPlay();
    });

    prevButton?.addEventListener("click", function () {
      showSlide(currentIndex - 1);
      restartAutoPlay();
    });

    dots.forEach(function (dot, dotIndex) {
      dot.addEventListener("click", function () {
        showSlide(dotIndex);
        restartAutoPlay();
      });
    });

    root.addEventListener("mouseenter", stopAutoPlay);
    root.addEventListener("mouseleave", startAutoPlay);

    function showSlide(nextIndex) {
      const normalizedIndex = normalizeIndex(nextIndex, slides.length);

      slides[currentIndex]?.classList.remove(ACTIVE_CLASS);
      dots[currentIndex]?.classList.remove(ACTIVE_CLASS);

      currentIndex = normalizedIndex;

      slides[currentIndex]?.classList.add(ACTIVE_CLASS);
      dots[currentIndex]?.classList.add(ACTIVE_CLASS);
    }

    function startAutoPlay() {
      if (slides.length < 2) {
        return;
      }

      stopAutoPlay();

      timerId = window.setInterval(function () {
        showSlide(currentIndex + 1);
      }, intervalMs);
    }

    function stopAutoPlay() {
      if (!timerId) {
        return;
      }

      window.clearInterval(timerId);
      timerId = null;
    }

    function restartAutoPlay() {
      stopAutoPlay();
      startAutoPlay();
    }
  }

  function normalizeIndex(index, itemCount) {
    return (index + itemCount) % itemCount;
  }

  function initRevealSections() {
    const sections = document.querySelectorAll(SELECTORS.revealSection);

    if (sections.length === 0) {
      return;
    }

    if (!("IntersectionObserver" in window)) {
      sections.forEach(function (section) {
        section.classList.add(VISIBLE_CLASS);
      });

      return;
    }

    const observer = new IntersectionObserver(
      function (entries) {
        entries.forEach(function (entry) {
          if (entry.isIntersecting) {
            entry.target.classList.add(VISIBLE_CLASS);
            observer.unobserve(entry.target);
          }
        });
      },
      { threshold: 0.12 },
    );

    sections.forEach(function (section) {
      observer.observe(section);
    });
  }

  function initBackToTop() {
    const backToTopButton = document.querySelector(SELECTORS.backToTop);

    if (!backToTopButton) {
      return;
    }

    updateBackToTopVisibility();

    window.addEventListener("scroll", updateBackToTopVisibility, {
      passive: true,
    });

    backToTopButton.addEventListener("click", scrollToTopSmoothly);

    function updateBackToTopVisibility() {
      backToTopButton.classList.toggle(VISIBLE_CLASS, window.scrollY > 600);
    }
  }

  function scrollToTopSmoothly() {
    const startPosition = window.scrollY;
    const duration = 950;
    const startTime = performance.now();
    const root = document.documentElement;
    const previousScrollBehavior = root.style.scrollBehavior;

    root.style.scrollBehavior = "auto";

    function easeOutCubic(progress) {
      return 1 - Math.pow(1 - progress, 3);
    }

    function animateScroll(currentTime) {
      const elapsed = currentTime - startTime;
      const progress = Math.min(elapsed / duration, 1);
      const easedProgress = easeOutCubic(progress);

      window.scrollTo(0, startPosition * (1 - easedProgress));

      if (progress < 1) {
        window.requestAnimationFrame(animateScroll);
        return;
      }

      root.style.scrollBehavior = previousScrollBehavior;
    }

    window.requestAnimationFrame(animateScroll);
  }

  function initConsultRequestForm() {
    const form = document.querySelector(SELECTORS.consultForm);
    const messageBox = document.querySelector(SELECTORS.consultMessage);

    if (!form) {
      return;
    }

    const submitButton = form.querySelector('button[type="submit"]');
    const defaultSubmitText =
      submitButton?.dataset.submitText ||
      submitButton?.textContent ||
      "ثبت درخواست مشاوره";

    form.addEventListener("submit", async function (event) {
      event.preventDefault();

      setSubmitState({
        submitButton,
        messageBox,
        isSubmitting: true,
        text: "در حال ثبت درخواست...",
      });

      try {
        const response = await fetch("/api/consult-requests", {
          method: "POST",
          headers: {
            "Content-Type": "application/json",
          },
          body: JSON.stringify(getConsultFormPayload(form)),
        });

        const result = await readJsonSafely(response);

        if (!response.ok) {
          throw new Error(result?.title || "خطا در ثبت درخواست");
        }

        const successMessage =
          form.dataset.successMessage ||
          result?.message ||
          "درخواست شما با موفقیت ثبت شد.";

        showMessage(messageBox, successMessage, "success");

        form.reset();
      } catch (error) {
        showMessage(
          messageBox,
          "ثبت درخواست با خطا مواجه شد. لطفاً دوباره تلاش کنید.",
          "error",
        );

        console.error(error);
      } finally {
        setSubmitState({
          submitButton,
          messageBox: null,
          isSubmitting: false,
          text: defaultSubmitText,
        });
      }
    });
  }

  function getConsultFormPayload(form) {
    const formData = new FormData(form);

    return {
      formName: formData.get("formName")?.toString().trim() || "consult_form",
      fullName: formData.get("fullName")?.toString().trim() || "",
      mobile: formData.get("mobile")?.toString().trim() || "",
      requestType: formData.get("requestType")?.toString().trim() || "",
      message: formData.get("message")?.toString().trim() || "",
    };
  }

  async function readJsonSafely(response) {
    try {
      return await response.json();
    } catch {
      return null;
    }
  }

  function setSubmitState({ submitButton, messageBox, isSubmitting, text }) {
    if (submitButton) {
      submitButton.disabled = isSubmitting;
      submitButton.textContent = text;
    }

    if (messageBox && isSubmitting) {
      showMessage(messageBox, text, null);
    }
  }

  function showMessage(messageBox, text, status) {
    if (!messageBox) {
      return;
    }

    messageBox.textContent = text;
    messageBox.className = status ? `form-message ${status}` : "form-message";
  }

  function initHeaderScrollState() {
    updateHeaderState();

    window.addEventListener("scroll", updateHeaderState, {
      passive: true,
    });

    function updateHeaderState() {
      document.body.classList.toggle(SCROLLED_CLASS, window.scrollY > 40);
    }
  }
})();

function initSmoothAnchorLinks() {
  const anchorLinks = document.querySelectorAll('a[href^="#"]:not([href="#"])');

  anchorLinks.forEach(function (link) {
    link.addEventListener("click", function (event) {
      const targetId = link.getAttribute("href");

      if (!targetId) {
        return;
      }

      const targetElement = document.getElementById(targetId.replace("#", ""));

      if (!targetElement) {
        return;
      }

      event.preventDefault();

      scrollToElementSmoothly(targetElement, 1000);

      window.history.pushState(null, "", targetId);
    });
  });
}

function scrollToElementSmoothly(targetElement, duration) {
  const header = document.querySelector(".site-header");
  const headerOffset = header ? header.offsetHeight + 18 : 0;

  const startPosition = window.scrollY;
  const targetPosition =
    targetElement.getBoundingClientRect().top + window.scrollY - headerOffset;

  const distance = targetPosition - startPosition;
  const startTime = performance.now();

  const root = document.documentElement;
  const previousScrollBehavior = root.style.scrollBehavior;

  root.style.scrollBehavior = "auto";

  function easeOutCubic(progress) {
    return 1 - Math.pow(1 - progress, 3);
  }

  function animateScroll(currentTime) {
    const elapsed = currentTime - startTime;
    const progress = Math.min(elapsed / duration, 1);
    const easedProgress = easeOutCubic(progress);

    window.scrollTo(0, startPosition + distance * easedProgress);

    if (progress < 1) {
      window.requestAnimationFrame(animateScroll);
      return;
    }

    root.style.scrollBehavior = previousScrollBehavior;
  }

  window.requestAnimationFrame(animateScroll);
}

function initMobileMenu() {
  const toggleButton = document.querySelector(".mobile-menu-toggle");
  const mainNavigation = document.querySelector("#mainNavigation");

  if (!toggleButton || !mainNavigation) {
    return;
  }

  toggleButton.addEventListener("click", function () {
    const isOpen = document.body.classList.toggle("mobile-nav-open");
    toggleButton.setAttribute("aria-expanded", isOpen ? "true" : "false");
  });

  mainNavigation.querySelectorAll("a").forEach(function (link) {
    link.addEventListener("click", function () {
      document.body.classList.remove("mobile-nav-open");
      toggleButton.setAttribute("aria-expanded", "false");
    });
  });

  window.addEventListener("resize", function () {
    if (window.innerWidth > 980) {
      document.body.classList.remove("mobile-nav-open");
      toggleButton.setAttribute("aria-expanded", "false");
    }
  });
}
