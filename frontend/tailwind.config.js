/** Theme-aware colour backed by the RGB channel tokens in `src/styles/_tokens.scss`. */
const token = (name) => `rgb(var(--rgb-${name}) / <alpha-value>)`;
const scale = (name) =>
  Object.fromEntries([50, 100, 200, 300, 400, 500, 600, 700, 800, 900].map((s) => [s, token(`${name}-${s}`)]));

/** @type {import('tailwindcss').Config} */
module.exports = {
  content: ['./src/**/*.{html,ts}'],
  // ui-badge builds `ui-badge-${variant}` at runtime, so the scanner never sees these names.
  safelist: [{ pattern: /^ui-badge-(neutral|success|warning|danger|info|brand|citrus|lime)$/ }],
  darkMode: 'class',
  // Preflight is disabled so Tailwind's base resets never clobber Angular Material
  // (MDC) components or the existing legacy global styles during the incremental migration.
  corePlugins: {
    preflight: false,
  },
  theme: {
    container: {
      center: true,
      padding: {
        DEFAULT: '1rem',
        sm: '1.5rem',
        lg: '2rem',
        xl: '2.5rem',
      },
      screens: {
        sm: '640px',
        md: '768px',
        lg: '1024px',
        xl: '1280px',
        '2xl': '1440px',
      },
    },
    screens: {
      xs: '376px',
      sm: '640px',
      md: '768px',
      lg: '1024px',
      xl: '1280px',
      '2xl': '1536px',
      '3xl': '1920px',
    },
    extend: {
      colors: {
        // Forest brand (trust / buy actions).
        brand: { ...scale('brand'), DEFAULT: token('brand-700') },
        // Neutral ink scale; inverts in dark mode.
        ink: scale('ink'),
        lime: {
          DEFAULT: token('lime'),
          soft: token('lime-soft'),
          deep: token('lime-deep'),
          fg: token('lime-fg'),
        },
        citrus: {
          DEFAULT: token('citrus'),
          soft: token('citrus-soft'),
          fg: token('citrus-fg'),
        },
        stage: token('stage'),
        success: { DEFAULT: token('success'), soft: token('success-soft'), fg: token('success-fg') },
        warning: { DEFAULT: token('warning'), soft: token('warning-soft'), fg: token('warning-fg') },
        danger: { DEFAULT: token('danger'), soft: token('danger-soft'), fg: token('danger-fg') },
        info: { DEFAULT: token('info'), soft: token('info-soft'), fg: token('info-fg') },
        surface: {
          DEFAULT: token('surface'),
          muted: token('surface-muted'),
          raised: token('surface-raised'),
        },
        // Modal backdrops and image overlays.
        scrim: token('scrim'),
      },
      zIndex: {
        header: '30',
        drawer: '50',
        modal: '100',
        toast: '110',
      },
      fontFamily: {
        sans: ['Manrope', 'system-ui', '-apple-system', 'Segoe UI', 'sans-serif'],
        display: ['Outfit', 'Manrope', 'system-ui', 'sans-serif'],
        arabic: ['"IBM Plex Sans Arabic"', 'Tajawal', 'system-ui', 'sans-serif'],
      },
      borderRadius: {
        lg: '0.625rem',
        xl: '1rem',
        '2xl': '1rem',
        '3xl': '1.5rem',
      },
      boxShadow: {
        xs: '0 1px 2px 0 rgb(10 28 22 / 0.05)',
        sm: '0 1px 3px 0 rgb(10 28 22 / 0.08), 0 1px 2px -1px rgb(10 28 22 / 0.06)',
        card: '0 1px 2px 0 rgb(10 28 22 / 0.05), 0 10px 24px -8px rgb(10 28 22 / 0.08)',
        elevated:
          '0 8px 16px -8px rgb(10 28 22 / 0.08), 0 18px 40px -14px rgb(10 28 22 / 0.14)',
        overlay: '0 24px 48px -12px rgb(10 28 22 / 0.28)',
        focus: '0 0 0 3px rgb(11 106 86 / 0.35)',
      },
      keyframes: {
        'fade-in': {
          from: { opacity: '0' },
          to: { opacity: '1' },
        },
        'slide-up': {
          from: { opacity: '0', transform: 'translateY(8px)' },
          to: { opacity: '1', transform: 'translateY(0)' },
        },
        'slide-in-end': {
          from: { transform: 'translateX(100%)' },
          to: { transform: 'translateX(0)' },
        },
        'sheet-up': {
          from: { transform: 'translateY(100%)' },
          to: { transform: 'translateY(0)' },
        },
        shimmer: {
          '100%': { transform: 'translateX(100%)' },
        },
      },
      animation: {
        'fade-in': 'fade-in 0.2s ease-out',
        'slide-up': 'slide-up 0.25s ease-out',
        'slide-in-end': 'slide-in-end 0.25s ease-out',
        'sheet-up': 'sheet-up 0.3s cubic-bezier(0.32, 0.72, 0, 1)',
      },
    },
  },
  plugins: [],
};
