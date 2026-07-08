/** @type {import('tailwindcss').Config} */
module.exports = {
  content: ['./src/**/*.{html,ts}'],
  corePlugins: {
    preflight: false, // prevents conflict with Angular Material CSS resets
  },
  theme: {
    extend: {
      // All colors reference the CSS custom properties defined in
      // src/styles.css :root — single source of truth for the palette.
      colors: {
        brand:        'rgb(var(--es-brand-rgb) / <alpha-value>)',
        'brand-dark': 'var(--es-brand-dark)',
        'brand-deep': 'var(--es-brand-deep)',
        primary:      'var(--mat-sys-primary)',
        hero: {
          start: 'var(--es-hero-start)',
          mid:   'var(--es-hero-mid)',
          end:   'var(--es-hero-end)',
        },
        es: {
          bg:             'var(--es-bg)',
          subtle:         'var(--es-bg-subtle)',
          border:         'var(--es-border)',
          'border-light': 'var(--es-border-light)',
          'border-strong':'var(--es-border-strong)',
          divider:        'var(--es-divider)',
          text:           'var(--es-text)',
          muted:          'var(--es-text-secondary)',
          soft:           'var(--es-text-soft)',
          faint:          'var(--es-text-muted)',
          success:        'var(--es-success)',
          danger:         'var(--es-danger)',
          'danger-bg':    'var(--es-red-wash)',
          warning:        'var(--es-warning)',
          warn:           'var(--es-warning-deep)',
          sky:            'var(--es-sky)',
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
};
