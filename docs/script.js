/**
 * BigFloat Documentation Site JavaScript
 */

// DOM Ready
document.addEventListener('DOMContentLoaded', function() {
    initTheme();
    initNavigation();
    initTabs();
    initCopyButtons();
    initSyntaxHighlighting();
});

/**
 * Theme Management
 */
function initTheme() {
    const themeToggle = document.getElementById('themeToggle');
    const sunIcon = themeToggle?.querySelector('.sun-icon');
    const moonIcon = themeToggle?.querySelector('.moon-icon');
    
    // Check for saved theme preference or default to light
    const savedTheme = localStorage.getItem('theme') || 'light';
    document.documentElement.setAttribute('data-theme', savedTheme);
    updateThemeIcon(savedTheme);
    
    // Theme toggle click handler
    themeToggle?.addEventListener('click', function() {
        const currentTheme = document.documentElement.getAttribute('data-theme');
        const newTheme = currentTheme === 'light' ? 'dark' : 'light';
        
        document.documentElement.setAttribute('data-theme', newTheme);
        localStorage.setItem('theme', newTheme);
        updateThemeIcon(newTheme);
    });
    
    function updateThemeIcon(theme) {
        if (!sunIcon || !moonIcon) return;
        
        if (theme === 'dark') {
            sunIcon.style.display = 'none';
            moonIcon.style.display = 'block';
        } else {
            sunIcon.style.display = 'block';
            moonIcon.style.display = 'none';
        }
    }
}

/**
 * Navigation
 */
function initNavigation() {
    const hamburger = document.getElementById('hamburger');
    const navLinks = document.getElementById('navLinks');
    const navbar = document.getElementById('navbar');
    
    // Hamburger menu toggle
    hamburger?.addEventListener('click', function() {
        navLinks?.classList.toggle('active');
        hamburger.classList.toggle('active');
        
        // Animate hamburger
        const spans = hamburger.querySelectorAll('span');
        if (hamburger.classList.contains('active')) {
            spans[0].style.transform = 'rotate(45deg) translate(5px, 5px)';
            spans[1].style.opacity = '0';
            spans[2].style.transform = 'rotate(-45deg) translate(7px, -6px)';
        } else {
            spans[0].style.transform = 'none';
            spans[1].style.opacity = '1';
            spans[2].style.transform = 'none';
        }
    });
    
    // Close mobile menu when clicking outside
    document.addEventListener('click', function(e) {
        if (!navbar?.contains(e.target) && navLinks?.classList.contains('active')) {
            navLinks.classList.remove('active');
            hamburger?.classList.remove('active');
            resetHamburger();
        }
    });
    
    // Close mobile menu when clicking on a link
    const navLinkItems = navLinks?.querySelectorAll('.nav-link');
    navLinkItems?.forEach(link => {
        link.addEventListener('click', function() {
            navLinks.classList.remove('active');
            hamburger?.classList.remove('active');
            resetHamburger();
        });
    });
    
    function resetHamburger() {
        const spans = hamburger?.querySelectorAll('span');
        if (spans) {
            spans[0].style.transform = 'none';
            spans[1].style.opacity = '1';
            spans[2].style.transform = 'none';
        }
    }
    
    // Active nav link highlighting based on current page
    const currentPath = window.location.pathname.split('/').pop() || 'index.html';
    const activeLink = document.querySelector(`.nav-link[href="${currentPath}"]`);
    if (activeLink) {
        activeLink.classList.add('active');
    }
}

/**
 * Tab Functionality
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
        });
    });
}

/**
 * Copy to Clipboard Functionality
 */
function initCopyButtons() {
    // Handle inline copy buttons
    document.querySelectorAll('.copy-btn').forEach(button => {
        button.addEventListener('click', function() {
            const codeElement = this.parentElement.querySelector('pre code');
            const textToCopy = codeElement ? codeElement.textContent : this.getAttribute('data-copy');
            
            copyToClipboard(textToCopy, this);
        });
    });
}

/**
 * Copy text to clipboard with fallback
 */
function copyToClipboard(text, button) {
    if (!text) return;
    
    if (navigator.clipboard && window.isSecureContext) {
        navigator.clipboard.writeText(text).then(() => {
            showCopyFeedback(button, true);
        }).catch(() => {
            fallbackCopy(text, button);
        });
    } else {
        fallbackCopy(text, button);
    }
}

/**
 * Fallback copy method for older browsers
 */
function fallbackCopy(text, button) {
    const textArea = document.createElement('textarea');
    textArea.value = text;
    textArea.style.position = 'fixed';
    textArea.style.left = '-999999px';
    document.body.appendChild(textArea);
    textArea.focus();
    textArea.select();
    
    try {
        document.execCommand('copy');
        showCopyFeedback(button, true);
    } catch (err) {
        showCopyFeedback(button, false);
    }
    
    document.body.removeChild(textArea);
}

/**
 * Show copy feedback
 */
function showCopyFeedback(button, success) {
    if (!button) return;
    
    const originalText = button.textContent;
    button.textContent = success ? 'Copied!' : 'Failed';
    button.style.backgroundColor = success ? '#10b981' : '#ef4444';
    button.style.color = 'white';
    button.style.borderColor = success ? '#10b981' : '#ef4444';
    
    setTimeout(() => {
        button.textContent = originalText;
        button.style.backgroundColor = '';
        button.style.color = '';
        button.style.borderColor = '';
    }, 2000);
}

/**
 * Basic Syntax Highlighting
 */
function initSyntaxHighlighting() {
    const codeBlocks = document.querySelectorAll('pre code');

    codeBlocks.forEach(block => {
        const rawCode = block.textContent;

        // Step 1: Escape HTML (including quotes!)
        const escapeHtml = (str) =>
            str.replace(/&/g, '&amp;')
                .replace(/</g, '&lt;')
                .replace(/>/g, '&gt;')
                .replace(/"/g, '&quot;');

        let html = escapeHtml(rawCode);

        // Step 2: Strings — match both raw and escaped quotes
        html = html.replace(/(&quot;)([^]*?)(\1)/g,
            '<span style="color: #ce9178;">$1$2$3</span>');

        // Step 3: Single-line comments
        html = html.replace(/(\/\/.*)/g,
            '<span style="color: #6a9955;">$1</span>');

        // Step 4: Multi-line comments
        html = html.replace(/(\/\*[\s\S]*?\*\/)/g,
            '<span style="color: #6a9955;">$1</span>');

        // Step 5: Numbers
        html = html.replace(/\b\d+(\.\d+)?\b/g,
            '<span style="color: #b5cea8;">$&</span>');

        // Step 6: Keywords (after quotes are handled!)
        html = html.replace(
            /\b(class|struct|interface|enum|namespace|using|public|private|protected|internal|static|readonly|const|var|int|string|bool|float|double|decimal|void|return|if|else|for|foreach|while|do|switch|case|default|try|catch|finally|throw|new|this|base|null|true|false)\b/g,
            '<span style="color: #569cd6;">$1</span>'
        );

        block.innerHTML = html;
    });
}

/**
 * Smooth scroll for anchor links
 */
document.querySelectorAll('a[href^="#"]').forEach(anchor => {
    anchor.addEventListener('click', function(e) {
        e.preventDefault();
        const target = document.querySelector(this.getAttribute('href'));
        if (target) {
            target.scrollIntoView({
                behavior: 'smooth',
                block: 'start'
            });
        }
    });
});

/**
 * Global copy function for onclick handlers
 */
window.copyToClipboard = function(text) {
    const tempButton = document.createElement('button');
    copyToClipboard(text, tempButton);
};