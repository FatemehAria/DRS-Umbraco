(function () {
  const slider = document.querySelector("[data-slider]");
  if (slider) {
    const slides = [...slider.querySelectorAll(".hero-slide")];
    const dots = [...slider.querySelectorAll("[data-dots] button")];
    const next = slider.querySelector("[data-next]");
    const prev = slider.querySelector("[data-prev]");
    let index = 0;
    let timer;

    const goTo = (nextIndex) => {
      slides[index].classList.remove("is-active");
      dots[index].classList.remove("is-active");
      index = (nextIndex + slides.length) % slides.length;
      slides[index].classList.add("is-active");
      dots[index].classList.add("is-active");
    };

    const start = () => {
      timer = window.setInterval(() => goTo(index + 1), 3000);
    };

    const restart = () => {
      window.clearInterval(timer);
      start();
    };

    next?.addEventListener("click", () => {
      goTo(index + 1);
      restart();
    });
    prev?.addEventListener("click", () => {
      goTo(index - 1);
      restart();
    });
    dots.forEach((dot, i) =>
      dot.addEventListener("click", () => {
        goTo(i);
        restart();
      }),
    );
    start();
  }

  const sections = document.querySelectorAll(".reveal-section");
  const observer = new IntersectionObserver(
    (entries) => {
      entries.forEach((entry) => {
        if (entry.isIntersecting) {
          entry.target.classList.add("is-visible");
          observer.unobserve(entry.target);
        }
      });
    },
    { threshold: 0.12 },
  );
  sections.forEach((section) => observer.observe(section));

  const backToTop = document.querySelector("[data-back-to-top]");

  if (backToTop) {
    window.addEventListener("scroll", () => {
      backToTop.classList.toggle("is-visible", window.scrollY > 600);
    });

    const scrollToTopSmoothly = () => {
      const startPosition = window.scrollY;
      const duration = 950;
      const startTime = performance.now();

      const easeOutCubic = (progress) => {
        return 1 - Math.pow(1 - progress, 3);
      };

      const animateScroll = (currentTime) => {
        const elapsed = currentTime - startTime;
        const progress = Math.min(elapsed / duration, 1);
        const easedProgress = easeOutCubic(progress);

        window.scrollTo(0, startPosition * (1 - easedProgress));

        if (progress < 1) {
          requestAnimationFrame(animateScroll);
        }
      };

      requestAnimationFrame(animateScroll);
    };

    backToTop.addEventListener("click", scrollToTopSmoothly);
  }
})();

// For Saving Demo Form
document.addEventListener("DOMContentLoaded", function () {
  const form = document.getElementById("consultRequestForm");
  const messageBox = document.getElementById("consultRequestMessage");

  if (!form) {
    return;
  }

  const submitButton = form.querySelector('button[type="submit"]');

  form.addEventListener("submit", async function (event) {
    event.preventDefault();

    if (submitButton) {
      submitButton.disabled = true;
      submitButton.textContent = "در حال ثبت درخواست...";
    }

    if (messageBox) {
      messageBox.textContent = "در حال ثبت درخواست...";
      messageBox.className = "form-message";
    }

    const formData = new FormData(form);

    const payload = {
      fullName: formData.get("fullName"),
      mobile: formData.get("mobile"),
      requestType: formData.get("requestType"),
      message: formData.get("message"),
    };

    try {
      const response = await fetch("/api/consult-requests", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify(payload),
      });

      let result = null;

      try {
        result = await response.json();
      } catch {
        result = null;
      }

      if (!response.ok) {
        throw new Error(result?.title || "خطا در ثبت درخواست");
      }

      if (messageBox) {
        messageBox.textContent =
          result?.message || "درخواست شما با موفقیت ثبت شد.";
        messageBox.className = "form-message success";
      }

      form.reset();
    } catch (error) {
      if (messageBox) {
        messageBox.textContent =
          "ثبت درخواست با خطا مواجه شد. لطفاً دوباره تلاش کنید.";
        messageBox.className = "form-message error";
      }

      console.error(error);
    } finally {
      if (submitButton) {
        submitButton.disabled = false;
        submitButton.textContent =
          submitButton.dataset.submitText || "ثبت درخواست مشاوره";
      }
    }
  });
});

// Testimonial Slider
document.addEventListener("DOMContentLoaded", function () {
  const testimonialRoot = document.querySelector("[data-testimonials]");

  if (!testimonialRoot) {
    return;
  }

  const slides = [...testimonialRoot.querySelectorAll(".testimonial-slide")];
  const dots = [
    ...testimonialRoot.querySelectorAll("[data-testimonial-dots] button"),
  ];
  const prev = testimonialRoot.querySelector("[data-testimonial-prev]");
  const next = testimonialRoot.querySelector("[data-testimonial-next]");

  if (slides.length === 0) {
    return;
  }

  let index = 0;
  let timer;

  const goTo = function (nextIndex) {
    slides[index].classList.remove("is-active");

    if (dots[index]) {
      dots[index].classList.remove("is-active");
    }

    index = (nextIndex + slides.length) % slides.length;

    slides[index].classList.add("is-active");

    if (dots[index]) {
      dots[index].classList.add("is-active");
    }
  };

  const start = function () {
    timer = window.setInterval(function () {
      goTo(index + 1);
    }, 5000);
  };

  const restart = function () {
    window.clearInterval(timer);
    start();
  };

  if (prev) {
    prev.addEventListener("click", function () {
      goTo(index - 1);
      restart();
    });
  }

  if (next) {
    next.addEventListener("click", function () {
      goTo(index + 1);
      restart();
    });
  }

  dots.forEach(function (dot, dotIndex) {
    dot.addEventListener("click", function () {
      goTo(dotIndex);
      restart();
    });
  });

  start();
});

// Header scroll state
document.addEventListener("DOMContentLoaded", function () {
  const updateHeaderState = function () {
    document.body.classList.toggle("is-scrolled", window.scrollY > 40);
  };

  updateHeaderState();
  window.addEventListener("scroll", updateHeaderState);
});
