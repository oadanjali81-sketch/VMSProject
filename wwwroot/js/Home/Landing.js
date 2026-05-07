// =====================
// Preloader logic
// =====================
const preloader = document.getElementById('preloader');
const hidePreloader = () => {
    if (preloader && !preloader.classList.contains('fade-out')) {
        preloader.classList.add('fade-out');
        document.body.style.overflow = 'auto';
    }
};

if (preloader) {
    const forceHide = setTimeout(hidePreloader, 2500);
    window.addEventListener('load', () => {
        clearTimeout(forceHide);
        setTimeout(hidePreloader, 400);
    });
}

// =====================
// GSAP Scroll Animations
// =====================
const initGSAP = () => {
    if (typeof gsap !== 'undefined') {
        gsap.registerPlugin(ScrollTrigger);

        // Universal card entrance — covers all card types
        const allCards = gsap.utils.toArray(
            '.feature-card, .benefit-card, .transform-card, .testimonial-card, .hero-stat'
        );
        allCards.forEach((el, i) => {
            gsap.from(el, {
                scrollTrigger: {
                    trigger: el,
                    start: 'top 92%',
                    toggleActions: 'play none none none'
                },
                y: 50,
                opacity: 0,
                duration: 0.7,
                delay: (i % 4) * 0.08,
                ease: 'power2.out'
            });
        });

        // Section titles slide up
        gsap.utils.toArray('.section-title, .section-subtitle').forEach(el => {
            gsap.from(el, {
                scrollTrigger: { trigger: el, start: 'top 90%', toggleActions: 'play none none none' },
                y: 30,
                opacity: 0,
                duration: 0.8,
                ease: 'power3.out'
            });
        });

        // Hero left panel slide-in
        gsap.from('.hero-left', {
            duration: 1.2,
            x: -60,
            opacity: 0,
            ease: 'power3.out',
            clearProps: 'all'
        });

        // Hero right slider slide-in
        gsap.from('.hero-image-wrapper', {
            duration: 1.2,
            x: 60,
            opacity: 0,
            ease: 'power3.out',
            delay: 0.2,
            clearProps: 'all'
        });

        // CTA section — animate text only, never opacity-hide the button
        gsap.from('.cta h2, .cta p:not(:last-child)', {
            scrollTrigger: { trigger: '.cta', start: 'top 85%', toggleActions: 'play none none none' },
            y: 30,
            opacity: 0,
            duration: 0.8,
            stagger: 0.15,
            ease: 'power2.out'
        });

        // Refresh after images settle
        setTimeout(() => ScrollTrigger.refresh(), 1000);
    }
};

// =====================
// Hero Image Slider + Floating Cards
// =====================
const slideWrappers = document.querySelectorAll('.hero-slide-wrapper');

// Each slide index maps to specific floating card IDs
const slideCards = {
    0: ['card-1'],       // Slide 1: Lobby Check-In
    1: ['card-2'],       // Slide 2: QR Security
    2: ['card-3']        // Slide 3: Dashboard
};

let currentSlide = 0;

function showSlide(index) {
    // Crossfade slides
    slideWrappers.forEach(s => s.classList.remove('active'));
    if (slideWrappers[index]) slideWrappers[index].classList.add('active');

    // Hide all floating cards first
    document.querySelectorAll('.floating-card').forEach(c => c.classList.remove('active'));

    // Show the cards mapped to this slide (with a slight delay for smooth entrance)
    const activeIds = slideCards[index] || [];
    activeIds.forEach((id, i) => {
        const card = document.getElementById(id);
        if (card) setTimeout(() => card.classList.add('active'), 200 + i * 150);
    });
}

// Start slider after page load
window.addEventListener('load', () => {
    // Show first slide + cards after preloader fades
    setTimeout(() => showSlide(0), 900);

    // Auto-rotate every 4 seconds
    setInterval(() => {
        currentSlide = (currentSlide + 1) % slideWrappers.length;
        showSlide(currentSlide);
    }, 4000);
});

// =====================
// Navbar scroll effect
// =====================
const handleNavbar = () => {
    const navbar = document.getElementById('navbar');
    if (navbar) {
        navbar.classList.toggle('scrolled', window.scrollY > 20);
    }
};
window.addEventListener('scroll', handleNavbar);

// =====================
// Smooth scroll for anchor links
// =====================
document.querySelectorAll('a[href^="#"]').forEach(anchor => {
    anchor.addEventListener('click', function (e) {
        e.preventDefault();
        const targetId = this.getAttribute('href').split('#')[1];
        const target = document.getElementById(targetId);
        if (target) target.scrollIntoView({ behavior: 'smooth', block: 'start' });
    });
});

// =====================
// Initialize GSAP
// =====================
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', initGSAP);
} else {
    initGSAP();
}
