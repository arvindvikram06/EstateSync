/** @type {import('tailwindcss').Config} */
module.exports = {
  content: ['./src/**/*.{html,ts}'],
  corePlugins: {
    preflight: false, // prevents conflict with Angular Material CSS resets
  },
  theme: {
    extend: {
      colors: {
        brand:        '#FF385C',
        'brand-dark': '#D4145A',
        es: {
          bg:     '#F7F7F7',
          border: '#DDDDDD',
          text:   '#222222',
          muted:  '#717171',
        },
      },
      borderRadius: {
        card: '16px',
      },
      boxShadow: {
        card:          '0 1px 2px rgba(0,0,0,0.06), 0 4px 12px rgba(0,0,0,0.08)',
        'card-hover':  '0 8px 28px rgba(0,0,0,0.16)',
      },
    },
  },
  plugins: [],
};
