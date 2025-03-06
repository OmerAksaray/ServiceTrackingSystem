// Service Tracking System - Modern UI Interactions

document.addEventListener('DOMContentLoaded', function() {
    // Initialize animations for elements with animation classes
    initAnimations();
    
    // Add hover effects to buttons
    initButtonEffects();
    
    // Initialize scroll effects
    initScrollEffects();
});

// Initialize animations for elements with animation classes
function initAnimations() {
    // Fade in elements with the fade-in class
    const fadeElements = document.querySelectorAll('.fade-in');
    fadeElements.forEach(element => {
        element.style.opacity = '0';
        setTimeout(() => {
            element.style.opacity = '1';
            element.style.transition = 'opacity 0.5s ease-in';
        }, 100);
    });
    
    // Slide up elements with the slide-up class
    const slideElements = document.querySelectorAll('.slide-up');
    slideElements.forEach(element => {
        element.style.transform = 'translateY(20px)';
        element.style.opacity = '0';
        setTimeout(() => {
            element.style.transform = 'translateY(0)';
            element.style.opacity = '1';
            element.style.transition = 'transform 0.5s ease-out, opacity 0.5s ease-out';
        }, 100);
    });
}

// Add hover effects to buttons
function initButtonEffects() {
    const buttons = document.querySelectorAll('.btn');
    buttons.forEach(button => {
        button.addEventListener('mouseover', function() {
            this.style.transform = 'translateY(-2px)';
        });
        
        button.addEventListener('mouseout', function() {
            this.style.transform = 'translateY(0)';
        });
    });
}

// Initialize scroll effects
function initScrollEffects() {
    // Add scroll observer for elements that should animate on scroll
    const observerOptions = {
        root: null,
        rootMargin: '0px',
        threshold: 0.1
    };
    
    const observer = new IntersectionObserver((entries, observer) => {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                entry.target.classList.add('visible');
                observer.unobserve(entry.target);
            }
        });
    }, observerOptions);
    
    const scrollElements = document.querySelectorAll('.scroll-animate');
    scrollElements.forEach(element => {
        observer.observe(element);
    });
    
    // Add smooth scroll for anchor links
    document.querySelectorAll('a[href^="#"]').forEach(anchor => {
        anchor.addEventListener('click', function(e) {
            e.preventDefault();
            
            const targetId = this.getAttribute('href');
            if (targetId === '#') return;
            
            const targetElement = document.querySelector(targetId);
            if (targetElement) {
                targetElement.scrollIntoView({
                    behavior: 'smooth'
                });
            }
        });
    });
}
