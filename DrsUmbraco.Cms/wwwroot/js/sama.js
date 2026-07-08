(function () {
    const slider = document.querySelector('[data-slider]');
    if (slider) {
        const slides = [...slider.querySelectorAll('.hero-slide')];
        const dots = [...slider.querySelectorAll('[data-dots] button')];
        const next = slider.querySelector('[data-next]');
        const prev = slider.querySelector('[data-prev]');
        let index = 0;
        let timer;

        const goTo = (nextIndex) => {
            slides[index].classList.remove('is-active');
            dots[index].classList.remove('is-active');
            index = (nextIndex + slides.length) % slides.length;
            slides[index].classList.add('is-active');
            dots[index].classList.add('is-active');
        };

        const start = () => {
            timer = window.setInterval(() => goTo(index + 1), 3000);
        };

        const restart = () => {
            window.clearInterval(timer);
            start();
        };

        next?.addEventListener('click', () => { goTo(index + 1); restart(); });
        prev?.addEventListener('click', () => { goTo(index - 1); restart(); });
        dots.forEach((dot, i) => dot.addEventListener('click', () => { goTo(i); restart(); }));
        start();
    }

    const sections = document.querySelectorAll('.reveal-section');
    const observer = new IntersectionObserver((entries) => {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                entry.target.classList.add('is-visible');
                observer.unobserve(entry.target);
            }
        });
    }, { threshold: 0.12 });
    sections.forEach(section => observer.observe(section));

    const backToTop = document.querySelector('[data-back-to-top]');
    if (backToTop) {
        window.addEventListener('scroll', () => {
            backToTop.classList.toggle('is-visible', window.scrollY > 600);
        });
        backToTop.addEventListener('click', () => window.scrollTo({ top: 0, behavior: 'smooth' }));
    }
})();
