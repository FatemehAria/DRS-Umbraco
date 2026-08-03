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
      lazyBackgrounds: true,
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
    lazyBackgrounds = false,
  }) {
    const slides = Array.from(root.querySelectorAll(slideSelector));
    const dots = Array.from(root.querySelectorAll(dotSelector));
    const nextButton = root.querySelector(nextSelector);
    const prevButton = root.querySelector(prevSelector);

    if (slides.length === 0) {
      return;
    }

    let currentIndex = slides.findIndex(function (slide) {
      return slide.classList.contains(ACTIVE_CLASS);
    });

    if (currentIndex < 0) {
      currentIndex = 0;
    }

    let timerId = null;
    let latestChangeRequest = 0;

    setActiveState(currentIndex);
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

      /*
       * وقتی موس روی نقطه یک اسلاید می‌رود،
       * تصویر همان اسلاید کمی زودتر دانلود می‌شود.
       */
      if (lazyBackgrounds) {
        dot.addEventListener("mouseenter", function () {
          loadSlideBackground(slides[dotIndex]);
        });
      }
    });

    root.addEventListener("mouseenter", stopAutoPlay);
    root.addEventListener("mouseleave", startAutoPlay);

    /*
     * وقتی کاربر موس را روی فلش می‌برد،
     * تصویر مقصد کمی زودتر دانلود می‌شود.
     */
    if (lazyBackgrounds) {
      nextButton?.addEventListener("mouseenter", function () {
        const nextIndex = normalizeIndex(currentIndex + 1, slides.length);

        loadSlideBackground(slides[nextIndex]);
      });

      prevButton?.addEventListener("mouseenter", function () {
        const previousIndex = normalizeIndex(currentIndex - 1, slides.length);

        loadSlideBackground(slides[previousIndex]);
      });
    }

    async function showSlide(nextIndex) {
      const normalizedIndex = normalizeIndex(nextIndex, slides.length);

      if (normalizedIndex === currentIndex) {
        return;
      }

      const requestedChange = ++latestChangeRequest;
      const targetSlide = slides[normalizedIndex];

      const imageLoaded = await loadSlideBackground(targetSlide);

      /*
       * اگر تصویر لود نشد، اسلاید خالی نمایش داده نشود.
       */
      if (!imageLoaded) {
        return;
      }

      /*
       * ممکن است هنگام دانلود تصویر، کاربر روی اسلاید
       * دیگری کلیک کرده باشد. در این حالت درخواست قبلی
       * نباید اعمال شود.
       */
      if (requestedChange !== latestChangeRequest) {
        return;
      }

      currentIndex = normalizedIndex;
      setActiveState(currentIndex);
    }

    function setActiveState(activeIndex) {
      slides.forEach(function (slide, slideIndex) {
        const isActive = slideIndex === activeIndex;

        slide.classList.toggle(ACTIVE_CLASS, isActive);

        if (lazyBackgrounds) {
          slide.setAttribute("aria-hidden", isActive ? "false" : "true");
        }
      });

      dots.forEach(function (dot, dotIndex) {
        const isActive = dotIndex === activeIndex;

        dot.classList.toggle(ACTIVE_CLASS, isActive);

        if (isActive) {
          dot.setAttribute("aria-current", "true");
        } else {
          dot.removeAttribute("aria-current");
        }
      });
    }

    function loadSlideBackground(slide) {
      /*
       * برای Testimonials و اسلایدرهایی که تصویر
       * پس‌زمینه ندارند، کاری انجام نمی‌دهیم.
       */
      if (!lazyBackgrounds) {
        return Promise.resolve(true);
      }

      const imageUrl = slide.dataset.backgroundUrl;

      /*
       * اسلاید اول background-image دارد، یا تصویر این
       * اسلاید قبلاً دانلود و data attribute حذف شده است.
       */
      if (!imageUrl) {
        return Promise.resolve(true);
      }

      /*
       * اگر دانلود همین تصویر قبلاً شروع شده، همان Promise
       * را برمی‌گردانیم تا درخواست تکراری ساخته نشود.
       */
      if (slide.backgroundLoadPromise) {
        return slide.backgroundLoadPromise;
      }

      slide.backgroundLoadPromise = new Promise(function (resolve) {
        const image = new Image();

        image.onload = function () {
          slide.style.backgroundImage = `url("${imageUrl}")`;

          slide.removeAttribute("data-background-url");

          resolve(true);
        };

        image.onerror = function () {
          console.error("Hero image could not be loaded:", imageUrl);

          /*
           * اجازه می‌دهیم در تلاش بعدی دوباره دانلود شود.
           */
          delete slide.backgroundLoadPromise;

          resolve(false);
        };

        image.src = imageUrl;
      });

      return slide.backgroundLoadPromise;
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
          body: getConsultFormPayload(form),
        });

        const result = await readJsonSafely(response);

        if (response.status === 429) {
          const retryAfterSeconds = Number(response.headers.get("Retry-After"));

          const retryMessage =
            Number.isFinite(retryAfterSeconds) && retryAfterSeconds > 0
              ? ` لطفاً حدود ${Math.ceil(retryAfterSeconds / 60)} دقیقه دیگر تلاش کنید.`
              : " لطفاً چند دقیقه دیگر تلاش کنید.";

          showMessage(
            messageBox,
            result?.detail ||
              `تعداد درخواست‌های شما بیش از حد مجاز است.${retryMessage}`,
            "error",
          );

          return;
        }

        if (!response.ok) {
          const validationMessage = result?.errors
            ? Object.values(result.errors).flat().find(Boolean)
            : null;

          throw new Error(
            validationMessage ||
              result?.detail ||
              result?.title ||
              "خطا در ثبت درخواست",
          );
        }

        const successMessage =
          form.dataset.successMessage ||
          result?.message ||
          "درخواست شما با موفقیت ثبت شد.";

        showMessage(messageBox, successMessage, "success");

        form.reset();
      } catch (error) {
        const message =
          error instanceof Error
            ? error.message
            : "ثبت درخواست با خطا مواجه شد. لطفاً دوباره تلاش کنید.";

        showMessage(messageBox, message, "error");
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

  // function getConsultFormPayload(form) {
  //   const formData = new FormData(form);

  //   return {
  //     formName: formData.get("formName")?.toString().trim() || "consult_form",
  //     fullName: formData.get("fullName")?.toString().trim() || "",
  //     mobile: formData.get("mobile")?.toString().trim() || "",
  //     requestType: formData.get("requestType")?.toString().trim() || "",
  //     message: formData.get("message")?.toString().trim() || "",
  //   };
  // }

  function getConsultFormPayload(form) {
    return new FormData(form);
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
