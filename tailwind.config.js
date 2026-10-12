/** @type {import('tailwindcss').Config} */
module.exports = {
  content: [
    "./Pages/**/*.cshtml",
    "./Pages/**/*.cs"
  ],
  theme: {
    extend: {
      colors: {
        /* ===== Design System (2026 Telecom / SaaS) ===== */

        /* legacy alias kept so existing markup (`text-navy`, `bg-navy`) keeps working –
           repointed to the new "ink" dark-slate used for text & dark surfaces */
        navy: {
          DEFAULT: "#0F172A",
          light: "#1E293B"
        },
        /* legacy alias kept so existing markup (`text-gold`, `bg-gold`) keeps working –
           repointed to the new primary-blue accent family */
        gold: {
          DEFAULT: "#2563EB",
          dark: "#1D4ED8",
          light: "#67E8F9"
        },

        /* new, explicit names for all new markup */
        primary: {
          DEFAULT: "#2563EB",
          dark: "#1D4ED8",
          50: "#EFF6FF",
          100: "#DBEAFE"
        },
        cyan: {
          DEFAULT: "#06B6D4",
          50: "#ECFEFF"
        },
        purple: {
          DEFAULT: "#8B5CF6",
          50: "#F5F3FF"
        },

        ink: "#0F172A",       /* Primary Text */
        secondary: "#64748B", /* Secondary Text */
        subtle: "#94A3B8",    /* Muted Text */

        surface: "#F8FAFC",   /* Main Background (legacy alias kept) */
        card: "#FFFFFF",      /* Card Background */
        line: "#E2E8F0",      /* Border */

        /* legacy alias kept so existing `text-muted` markup keeps working */
        muted: "#64748B",

        success: "#10B981",
        warning: "#F59E0B",
        danger: "#EF4444"
      },
      fontFamily: {
        sans: ["Vazirmatn", "Tahoma", "sans-serif"]
      },
      borderRadius: {
        card: "1rem",
        xl2: "1.15rem"
      },
      boxShadow: {
        soft: "0 1px 2px rgba(15,23,42,0.04), 0 12px 28px -14px rgba(15,23,42,0.12)",
        softer: "0 1px 1px rgba(15,23,42,0.03), 0 4px 12px -6px rgba(15,23,42,0.08)",
        lift: "0 20px 40px -16px rgba(37,99,235,0.22)",
        glow: "0 0 0 4px rgba(37,99,235,0.10)"
      },
      letterSpacing: {
        digits: "0.12em"
      },
      backgroundImage: {
        "hero-radial": "radial-gradient(circle at 100% 0%, rgba(6,182,212,0.10), transparent 45%), radial-gradient(circle at 0% 100%, rgba(37,99,235,0.10), transparent 45%)",
        "primary-soft": "linear-gradient(135deg, #2563EB 0%, #1D4ED8 100%)"
      },
      keyframes: {
        "fade-up": {
          "0%": { opacity: "0", transform: "translateY(8px)" },
          "100%": { opacity: "1", transform: "translateY(0)" }
        },
        shimmer: {
          "0%": { backgroundPosition: "-400px 0" },
          "100%": { backgroundPosition: "400px 0" }
        }
      },
      animation: {
        "fade-up": "fade-up .5s ease both",
        shimmer: "shimmer 1.6s infinite linear"
      }
    }
  },
  plugins: []
};
