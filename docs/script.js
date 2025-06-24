/**
 * BigFloat Library Website JavaScript
 * Enhanced by Claude (Anthropic) for modern interactivity
 */

// DOM Ready
document.addEventListener('DOMContentLoaded', function() {
    initNavigation();
    initTabs();
    initScrollEffects();
    initAnimations();
    initCopyButtons();
    initPerformanceCharts();
    initAccessibility();
});

/**
 * Navigation functionality
 */
function initNavigation() {
    const navbar = document.getElementById('navbar');
    const hamburger = document.getElementById('hamburger');
    const navLinks = document.getElementById('navLinks');
    const navLinkItems = navLinks.querySelectorAll('.nav-link');
    
    // Hamburger menu toggle
    hamburger.addEventListener('click', function() {
        navLinks.classList.toggle('active');
        hamburger.classList.toggle('active');
        
        // Animate hamburger
        const spans = hamburger.querySelectorAll('span');
        if (hamburger.classList.contains('active')) {
            spans[0].style.transform = 'rotate(45deg) translate(6px, 6px)';
            spans[1].style.opacity = '0';
            spans[2].style.transform = 'rotate(-45deg) translate(6px, -6px)';
        } else {
            spans[0].style.transform = 'none';
            spans[1].style.opacity = '1';
            spans[2].style.transform = 'none';
        }
    });
    
    // Close mobile menu when clicking on a link
    navLinkItems.forEach(link => {
        link.addEventListener('click', function() {
            navLinks.classList.remove('active');
            hamburger.classList.remove('active');
            
            // Reset hamburger animation
            const spans = hamburger.querySelectorAll('span');
            spans[0].style.transform = 'none';
            spans[1].style.opacity = '1';
            spans[2].style.transform = 'none';
        });
    });
    
    // Close mobile menu when clicking outside
    document.addEventListener('click', function(e) {
        if (!navbar.contains(e.target) && navLinks.classList.contains('active')) {
            navLinks.classList.remove('active');
            hamburger.classList.remove('active');
            
            // Reset hamburger animation
            const spans = hamburger.querySelectorAll('span');
            spans[0].style.transform = 'none';
            spans[1].style.opacity = '1';
            spans[2].style.transform = 'none';
        }
    });
    
    // Navbar scroll effects
    let lastScrollTop = 0;
    window.addEventListener('scroll', function() {
        const scrollTop = window.pageYOffset || document.documentElement.scrollTop;
        
        if (scrollTop > 100) {
            navbar.classList.add('scrolled');
        } else {
            navbar.classList.remove('scrolled');
        }
        
        lastScrollTop = scrollTop;
    });
    
    // Smooth scrolling for navigation links
    navLinkItems.forEach(link => {
        if (link.getAttribute('href').startsWith('#')) {
            link.addEventListener('click', function(e) {
                e.preventDefault();
                const targetId = this.getAttribute('href').substring(1);
                const targetElement = document.getElementById(targetId);
                
                if (targetElement) {
                    const offsetTop = targetElement.offsetTop - 80; // Account for fixed navbar
                    window.scrollTo({
                        top: offsetTop,
                        behavior: 'smooth'
                    });
                }
            });
        }
    });
    
    // Highlight active section in navigation
    const sections = document.querySelectorAll('section[id]');
    const observerOptions = {
        threshold: 0.3,
        rootMargin: '-80px 0px -50% 0px'
    };
    
    const observer = new IntersectionObserver(function(entries) {
        entries.forEach(entry => {
            const navLink = document.querySelector(`.nav-link[href="#${entry.target.id}"]`);
            if (navLink) {
                if (entry.isIntersecting) {
                    document.querySelectorAll('.nav-link').forEach(link => link.classList.remove('active'));
                    navLink.classList.add('active');
                }
            }
        });
    }, observerOptions);
    
    sections.forEach(section => observer.observe(section));
}

/**
 * Tab functionality for examples section
 */
function initTabs() {
    const tabButtons = document.querySelectorAll('.tab-btn');
    const tabPanes = document.querySelectorAll('.tab-pane');
    
    tabButtons.forEach(button => {
        button.addEventListener('click', function() {
            const targetTab = this.getAttribute('data-tab');
            
            // Remove active class from all buttons and panes
            tabButtons.forEach(btn => btn.classList.remove('active'));
            tabPanes.forEach(pane => pane.classList.remove('active'));
            
            // Add active class to clicked button and corresponding pane
            this.classList.add('active');
            const targetPane = document.getElementById(`${targetTab}-tab`);
            if (targetPane) {
                targetPane.classList.add('active');
            }
            
            // Trigger fade-in animation
            setTimeout(() => {
                if (targetPane) {
                    targetPane.style.opacity = '0';
                    targetPane.style.transform = 'translateY(20px)';
                    targetPane.style.transition = 'opacity 0.3s ease, transform 0.3s ease';
                    
                    requestAnimationFrame(() => {
                        targetPane.style.opacity = '1';
                        targetPane.style.transform = 'translateY(0)';
                    });
                }
            }, 50);
        });
    });
}

/**
 * Scroll effects and animations
 */
function initScrollEffects() {
    // Parallax effect for hero background
    const hero = document.querySelector('.hero');
    if (hero) {
        window.addEventListener('scroll', function() {
            const scrolled = window.pageYOffset;
            const rate = scrolled * -0.5;
            hero.style.transform = `translateY(${rate}px)`;
        });
    }
    
    // Fade-in animation for elements
    const observerOptions = {
        threshold: 0.1,
        rootMargin: '0px 0px -50px 0px'
    };
    
    const fadeObserver = new IntersectionObserver(function(entries) {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                entry.target.classList.add('fade-in-up');
                fadeObserver.unobserve(entry.target);
            }
        });
    }, observerOptions);
    
    // Observe elements for fade-in animation
    const animateElements = document.querySelectorAll(
        '.overview-card, .feature-card, .performance-card, .doc-card, .install-card'
    );
    animateElements.forEach(el => {
        el.style.opacity = '0';
        el.style.transform = 'translateY(30px)';
        el.style.transition = 'opacity 0.6s ease, transform 0.6s ease';
        fadeObserver.observe(el);
    });
}

/**
 * Initialize animations
 */
function initAnimations() {
    // Floating animation for code blocks
    const codeBlocks = document.querySelectorAll('.floating-code');
    codeBlocks.forEach((block, index) => {
        block.style.animationDelay = `${index * 0.5}s`;
    });
    
    // Typewriter effect for hero title
    const heroTitle = document.querySelector('.hero-title .gradient-text');
    if (heroTitle) {
        const text = heroTitle.textContent;
        heroTitle.textContent = '';
        heroTitle.style.borderRight = '2px solid';
        
        let i = 0;
        const typeWriter = () => {
            if (i < text.length) {
                heroTitle.textContent += text.charAt(i);
                i++;
                setTimeout(typeWriter, 100);
            } else {
                setTimeout(() => {
                    heroTitle.style.borderRight = 'none';
                }, 1000);
            }
        };
        
        // Start typewriter after a short delay
        setTimeout(typeWriter, 1000);
    }
    
    // Counter animation for statistics
    const stats = document.querySelectorAll('.stat-number');
    const statsObserver = new IntersectionObserver(function(entries) {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                animateCounter(entry.target);
                statsObserver.unobserve(entry.target);
            }
        });
    });
    
    stats.forEach(stat => statsObserver.observe(stat));
}

/**
 * Animate counter numbers
 */
function animateCounter(element) {
    const target = element.textContent;
    const isNumber = /^\d+/.test(target);
    
    if (isNumber) {
        const finalValue = parseInt(target.match(/\d+/)[0]);
        const suffix = target.replace(/^\d+/, '');
        let current = 0;
        const increment = finalValue / 50;
        const timer = setInterval(() => {
            current += increment;
            if (current >= finalValue) {
                element.textContent = finalValue + suffix;
                clearInterval(timer);
            } else {
                element.textContent = Math.floor(current) + suffix;
            }
        }, 30);
    }
}

/**
 * Copy to clipboard functionality
 */
function initCopyButtons() {
    const copyButtons = document.querySelectorAll('.copy-btn');
    
    copyButtons.forEach(button => {
        button.addEventListener('click', function() {
            const codeBlock = this.parentElement.querySelector('pre code');
            const text = codeBlock ? codeBlock.textContent : '';
            
            copyToClipboard(text);
            
            // Visual feedback
            const originalHTML = this.innerHTML;
            this.innerHTML = `
                <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                    <polyline points="20,6 9,17 4,12"/>
                </svg>
            `;
            this.style.background = '#10b981';
            
            setTimeout(() => {
                this.innerHTML = originalHTML;
                this.style.background = '';
            }, 2000);
        });
    });
}

/**
 * Copy text to clipboard
 */
function copyToClipboard(text) {
    if (navigator.clipboard && window.isSecureContext) {
        navigator.clipboard.writeText(text).then(() => {
            showNotification('Copied to clipboard!', 'success');
        }).catch(err => {
            console.error('Failed to copy: ', err);
            fallbackCopyTextToClipboard(text);
        });
    } else {
        fallbackCopyTextToClipboard(text);
    }
}

/**
 * Fallback copy method for older browsers
 */
function fallbackCopyTextToClipboard(text) {
    const textArea = document.createElement('textarea');
    textArea.value = text;
    textArea.style.position = 'fixed';
    textArea.style.left = '-999999px';
    textArea.style.top = '-999999px';
    document.body.appendChild(textArea);
    textArea.focus();
    textArea.select();
    
    try {
        document.execCommand('copy');
        showNotification('Copied to clipboard!', 'success');
    } catch (err) {
        console.error('Fallback copy failed: ', err);
        showNotification('Failed to copy to clipboard', 'error');
    }
    
    document.body.removeChild(textArea);
}

/**
 * Show notification
 */
function showNotification(message, type = 'info') {
    const notification = document.createElement('div');
    notification.className = `notification notification-${type}`;
    notification.textContent = message;
    
    notification.style.cssText = `
        position: fixed;
        top: 20px;
        right: 20px;
        background: ${type === 'success' ? '#10b981' : type === 'error' ? '#ef4444' : '#3b82f6'};
        color: white;
        padding: 12px 24px;
        border-radius: 8px;
        font-weight: 500;
        box-shadow: 0 10px 15px -3px rgba(0, 0, 0, 0.1);
        z-index: 9999;
        transform: translateX(100%);
        transition: transform 0.3s ease;
    `;
    
    document.body.appendChild(notification);
    
    // Animate in
    requestAnimationFrame(() => {
        notification.style.transform = 'translateX(0)';
    });
    
    // Auto remove
    setTimeout(() => {
        notification.style.transform = 'translateX(100%)';
        setTimeout(() => {
            if (notification.parentNode) {
                notification.parentNode.removeChild(notification);
            }
        }, 300);
    }, 3000);
}

/**
 * Initialize performance charts
 */
function initPerformanceCharts() {
    const chartBars = document.querySelectorAll('.bar');
    const precisionBars = document.querySelectorAll('.scale-fill');
    
    const chartObserver = new IntersectionObserver(function(entries) {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                // Animate chart bars
                if (entry.target.classList.contains('bar')) {
                    const width = entry.target.style.width || '50%';
                    entry.target.style.setProperty('--width', width);
                    
                    // Add animation class
                    entry.target.style.animation = 'barGrow 1s ease-out forwards';
                }
                
                // Animate precision scale bars
                if (entry.target.classList.contains('scale-fill')) {
                    const computedStyle = window.getComputedStyle(entry.target);
                    const width = computedStyle.width;
                    entry.target.style.width = '0%';
                    
                    setTimeout(() => {
                        entry.target.style.transition = 'width 1.5s ease-out';
                        entry.target.style.width = width;
                    }, 200);
                }
                
                chartObserver.unobserve(entry.target);
            }
        });
    });
    
    chartBars.forEach(bar => chartObserver.observe(bar));
    precisionBars.forEach(bar => chartObserver.observe(bar));
}

/**
 * Initialize accessibility features
 */
function initAccessibility() {
    // Add skip to main content link
    const skipLink = document.createElement('a');
    skipLink.href = '#main';
    skipLink.textContent = 'Skip to main content';
    skipLink.className = 'skip-link';
    skipLink.style.cssText = `
        position: absolute;
        top: -40px;
        left: 6px;
        background: #000;
        color: #fff;
        padding: 8px;
        text-decoration: none;
        border-radius: 4px;
        z-index: 10000;
        transition: top 0.3s;
    `;
    
    skipLink.addEventListener('focus', function() {
        this.style.top = '6px';
    });
    
    skipLink.addEventListener('blur', function() {
        this.style.top = '-40px';
    });
    
    document.body.insertBefore(skipLink, document.body.firstChild);
    
    // Add main landmark
    const main = document.querySelector('main');
    if (main) {
        main.id = 'main';
        main.setAttribute('role', 'main');
    }
    
    // Enhance keyboard navigation
    const focusableElements = document.querySelectorAll(
        'a, button, input, textarea, select, details, [tabindex]:not([tabindex="-1"])'
    );
    
    focusableElements.forEach(element => {
        element.addEventListener('keydown', function(e) {
            if (e.key === 'Enter' && this.tagName === 'BUTTON') {
                this.click();
            }
        });
    });
    
    // Add ARIA labels to interactive elements
    const tabButtons = document.querySelectorAll('.tab-btn');
    tabButtons.forEach((button, index) => {
        button.setAttribute('role', 'tab');
        button.setAttribute('aria-selected', button.classList.contains('active') ? 'true' : 'false');
        button.setAttribute('aria-controls', button.getAttribute('data-tab') + '-tab');
    });
    
    const tabPanes = document.querySelectorAll('.tab-pane');
    tabPanes.forEach(pane => {
        pane.setAttribute('role', 'tabpanel');
    });
}

/**
 * Initialize syntax highlighting (basic)
 */
function initSyntaxHighlighting() {
    const codeBlocks = document.querySelectorAll('pre code');
    
    codeBlocks.forEach(block => {
        let html = block.innerHTML;
        
        // Basic C# syntax highlighting
        html = html.replace(/\b(class|struct|interface|enum|namespace|using|public|private|protected|internal|static|readonly|const|var|int|string|bool|float|double|decimal|void|return|if|else|for|foreach|while|do|switch|case|default|try|catch|finally|throw|new|this|base|null|true|false)\b/g, '<span style="color: #569cd6;">$1</span>');
        
        // Strings
        html = html.replace(/"([^"\\]|\\.)*"/g, '<span style="color: #ce9178;">$&</span>');
        
        // Comments
        html = html.replace(/\/\/.*$/gm, '<span style="color: #6a9955;">$&</span>');
        html = html.replace(/\/\*[\s\S]*?\*\//g, '<span style="color: #6a9955;">$&</span>');
        
        // Numbers
        html = html.replace(/\b\d+(\.\d+)?\b/g, '<span style="color: #b5cea8;">$&</span>');
        
        block.innerHTML = html;
    });
}

/**
 * Debounce function for performance
 */
function debounce(func, wait) {
    let timeout;
    return function executedFunction(...args) {
        const later = () => {
            clearTimeout(timeout);
            func(...args);
        };
        clearTimeout(timeout);
        timeout = setTimeout(later, wait);
    };
}

/**
 * Throttle function for scroll events
 */
function throttle(func, limit) {
    let inThrottle;
    return function() {
        const args = arguments;
        const context = this;
        if (!inThrottle) {
            func.apply(context, args);
            inThrottle = true;
            setTimeout(() => inThrottle = false, limit);
        }
    };
}

// Global functions for HTML onclick handlers
window.copyToClipboard = copyToClipboard;

// Initialize syntax highlighting after DOM content is loaded
document.addEventListener('DOMContentLoaded', function() {
    initSyntaxHighlighting();
});

// Performance optimizations
window.addEventListener('scroll', throttle(function() {
    // Scroll-based animations are handled in initScrollEffects
}, 16)); // ~60fps

window.addEventListener('resize', debounce(function() {
    // Handle responsive adjustments if needed
    const mobileBreakpoint = 768;
    const isMobile = window.innerWidth <= mobileBreakpoint;
    
    if (isMobile) {
        // Mobile-specific adjustments
        document.body.classList.add('mobile');
    } else {
        document.body.classList.remove('mobile');
    }
}, 250));

// Initialize performance optimizations
window.addEventListener('load', function() {
    // Preload important images
    const importantImages = [
        // Add any critical images here
    ];
    
    importantImages.forEach(src => {
        const img = new Image();
        img.src = src;
    });
    
    // Remove loading class if present
    document.body.classList.remove('loading');
});

// Service Worker registration for PWA capabilities (optional)
if ('serviceWorker' in navigator) {
    window.addEventListener('load', function() {
        navigator.serviceWorker.register('/sw.js')
            .then(function(registration) {
                console.log('ServiceWorker registration successful');
            })
            .catch(function(err) {
                console.log('ServiceWorker registration failed');
            });
    });
}

// Add CSS animation keyframes programmatically
const style = document.createElement('style');
style.textContent = `
    @keyframes barGrow {
        from {
            width: 0%;
        }
        to {
            width: var(--width, 50%);
        }
    }
    
    .fade-in-up {
        opacity: 1 !important;
        transform: translateY(0) !important;
    }
    
    @media (prefers-reduced-motion: reduce) {
        *,
        *::before,
        *::after {
            animation-duration: 0.01ms !important;
            animation-iteration-count: 1 !important;
            transition-duration: 0.01ms !important;
            scroll-behavior: auto !important;
        }
    }
`;
document.head.appendChild(style);