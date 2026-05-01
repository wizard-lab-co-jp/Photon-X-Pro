/** @type {import('tailwindcss').Config} */
export default {
  content: ['./src/**/*.{html,js,svelte,ts}'],
  theme: {
    extend: {
      colors: {
        workspace: '#1e1e1e',
        toolbar: '#2b2b2b',
        panel: '#252525',
        'panel-border': '#3a3a3a',
        'accent-blue': '#0078d4',
        'accent-red': '#c41e3a',
        'text-primary': '#f0f0f0',
        'text-muted': '#9e9e9e',
      },
      fontSize: {
        'xxs': '0.65rem',
      }
    },
  },
  plugins: [],
}
