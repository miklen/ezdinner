<script setup lang="ts">
const { $msal } = useNuxtApp()

const featuresRef = ref<HTMLElement | null>(null)
const featuresVisible = ref(false)

onMounted(() => {
  if ($msal.isAuthenticated.value) navigateTo('/home')

  const observer = new IntersectionObserver(
    ([entry]) => {
      if (entry.isIntersecting) {
        featuresVisible.value = true
        observer.disconnect()
      }
    },
    { threshold: 0.1 },
  )
  if (featuresRef.value) observer.observe(featuresRef.value)
})

watch($msal.isAuthenticated, (val) => {
  if (val) navigateTo('/home')
})

const features = [
  {
    number: '01',
    icon: 'mdi-silverware-fork-knife',
    title: 'Build your catalog',
    text: 'Add every dish your family loves — with ratings, notes, and a recipe link. No more forgotten favourites.',
  },
  {
    number: '02',
    icon: 'mdi-calendar-week',
    title: 'Plan the week',
    text: 'Fill in the week\'s dinners in seconds. See what\'s coming and never scramble for ideas again.',
  },
  {
    number: '03',
    icon: 'mdi-chart-line',
    title: 'Discover patterns',
    text: 'Usage stats and ratings surface what\'s worth repeating. See your family\'s real favourites over time.',
  },
]
</script>

<template>
  <v-app>
    <!-- ── Navigation ──────────────────────────────────────────────── -->
    <v-app-bar :elevation="0" class="landing-nav" height="68">
      <v-container :fluid="false" class="py-0 fill-height nav-container">
        <span class="logotype">
          <span class="logotype__ez">Ez</span><span class="logotype__dinner">Dinner</span>
        </span>
        <v-spacer />
        <v-btn
          variant="outlined"
          class="signin-nav-btn"
          @click="$msal.signIn()"
        >
          Sign in
        </v-btn>
      </v-container>
    </v-app-bar>

    <v-main>
      <!-- ── Hero ───────────────────────────────────────────────────── -->
      <section class="hero-section">
        <v-container :fluid="false" class="hero-container">
          <div class="hero-grid">
            <!-- Text -->
            <div class="hero-content">
              <span class="eyebrow">Family dinner, simplified</span>
              <h1 class="hero-heading">
                Plan dinner.<br>
                <span class="hero-heading-accent">Together.</span>
              </h1>
              <p class="hero-body">
                EzDinner helps your family track favourite dishes, plan the week ahead,
                and put an end to the daily "what's for dinner?" scramble.
              </p>
              <div class="hero-actions">
                <v-btn
                  size="x-large"
                  variant="flat"
                  class="hero-cta"
                  @click="$msal.signIn()"
                >
                  Get started — it's free
                </v-btn>
                <span class="hero-hint">Create a free account to get started</span>
              </div>
            </div>

            <!-- Image -->
            <div class="hero-image-outer" aria-hidden="true">
              <div class="hero-image-frame">
                <img
                  src="/images/pexels-fauxels-3184183-l.jpg"
                  alt=""
                  class="hero-image"
                >
              </div>
              <!-- Floating dish badge -->
              <div class="hero-badge">
                <v-icon icon="mdi-heart" size="14" color="#E5A83B" />
                <span>Pasta Carbonara — tonight's dinner</span>
              </div>
              <!-- Decorative ring -->
              <div class="hero-ring" aria-hidden="true" />
            </div>
          </div>
        </v-container>
      </section>

      <!-- ── Features ───────────────────────────────────────────────── -->
      <section class="features-section">
        <v-container :fluid="false">
          <div class="features-header">
            <span class="eyebrow">How it works</span>
            <h2 class="features-heading">
              Everything a family needs
            </h2>
          </div>

          <div
            ref="featuresRef"
            class="features-grid"
            :class="{ 'is-visible': featuresVisible }"
          >
            <div
              v-for="(feature, i) in features"
              :key="i"
              class="feature-card"
              :style="`--stagger-delay: ${i * 110}ms`"
            >
              <span class="feature-num" aria-hidden="true">{{ feature.number }}</span>
              <div class="feature-icon-wrap">
                <v-icon :icon="feature.icon" class="feature-icon" />
              </div>
              <h3 class="feature-title">
                {{ feature.title }}
              </h3>
              <p class="feature-text">
                {{ feature.text }}
              </p>
            </div>
          </div>
        </v-container>
      </section>

      <!-- ── CTA ────────────────────────────────────────────────────── -->
      <section class="cta-section">
        <v-container :fluid="false">
          <div class="cta-card">
            <div class="cta-content">
              <h2 class="cta-heading">
                Ready to plan dinner?
              </h2>
              <p class="cta-sub">
                Create a free account and get started in seconds.
              </p>
            </div>
            <v-btn
              size="x-large"
              variant="flat"
              class="cta-btn"
              @click="$msal.signIn()"
            >
              Sign in to get started
            </v-btn>
          </div>
        </v-container>
      </section>
      <!-- Page slot: pages/index.vue renders here (empty div, no visual impact) -->
      <slot />
    </v-main>

    <!-- ── Footer ─────────────────────────────────────────────────── -->
    <footer class="landing-footer">
      <v-container :fluid="false" class="footer-inner">
        <span class="footer-logo">
          <span class="logotype__ez">Ez</span><span class="logotype__dinner footer-logo-dinner">Dinner</span>
        </span>
        <span class="footer-copy">&copy; {{ new Date().getFullYear() }} EzLifehacks</span>
      </v-container>
    </footer>
  </v-app>
</template>

<style scoped lang="scss">
// ── Nav ──────────────────────────────────────────────────────────────────────

.landing-nav {
  background-color: var(--color-background) !important;
  border-bottom: 1px solid var(--color-border) !important;
}

.nav-container {
  max-width: 1200px;
}

.logotype {
  display: inline-flex;
  align-items: baseline;
  line-height: 1;
  text-decoration: none;
  user-select: none;
}

.logotype__ez {
  font-family: var(--font-display);
  font-size: 26px;
  font-weight: 400;
  letter-spacing: -0.02em;
  color: var(--color-text-primary);
}

.logotype__dinner {
  font-family: var(--font-display);
  font-size: 26px;
  font-weight: 400;
  letter-spacing: -0.02em;
  color: var(--color-primary);
}

.signin-nav-btn {
  border-color: var(--color-border-medium) !important;
  color: var(--color-text-primary) !important;
  font-weight: 500;
  font-size: var(--text-sm);
  letter-spacing: 0;
  border-radius: var(--radius-md) !important;
  transition: border-color var(--duration-fast) var(--ease-out),
              background-color var(--duration-fast) var(--ease-out);

  &:hover {
    border-color: var(--color-primary) !important;
    background-color: rgba(212, 101, 42, 0.04) !important;
  }
}

// ── Hero ─────────────────────────────────────────────────────────────────────

.hero-section {
  background-color: var(--color-background);
  padding: 80px 0 96px;

  @media (max-width: 768px) {
    padding: 48px 0 64px;
  }
}

.hero-container {
  max-width: 1200px;
}

.hero-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 80px;
  align-items: center;

  @media (max-width: 960px) {
    grid-template-columns: 1fr;
    gap: 48px;
  }
}

// Hero content — animate on load
.hero-content {
  animation: fadeSlideUp var(--duration-slow) var(--ease-out) both;
}

.eyebrow {
  display: inline-block;
  font-family: var(--font-body);
  font-size: var(--text-xs);
  font-weight: 600;
  letter-spacing: 0.1em;
  text-transform: uppercase;
  color: var(--color-text-muted);
  margin-bottom: var(--space-4);
}

.hero-heading {
  font-family: var(--font-display);
  font-size: clamp(40px, 5.5vw, 64px);
  font-weight: 400;
  line-height: 1.05;
  letter-spacing: -0.03em;
  color: var(--color-text-primary);
  margin: 0 0 var(--space-6);
}

.hero-heading-accent {
  color: var(--color-primary);
}

.hero-body {
  font-family: var(--font-body);
  font-size: var(--text-lg);
  line-height: 1.65;
  color: var(--color-text-secondary);
  margin: 0 0 var(--space-8);
  max-width: 440px;
}

.hero-actions {
  display: flex;
  align-items: center;
  gap: var(--space-6);
  flex-wrap: wrap;
}

.hero-cta {
  background-color: var(--color-primary-dark) !important;
  color: var(--color-text-on-primary) !important;
  border-radius: var(--radius-md) !important;
  font-weight: 600 !important;
  font-size: var(--text-base) !important;
  letter-spacing: 0 !important;
  height: 52px !important;
  padding: 0 28px !important;
  box-shadow: 0 4px 12px rgba(184, 81, 29, 0.28) !important;
  transition: transform var(--duration-fast) var(--ease-out),
              box-shadow var(--duration-fast) var(--ease-out) !important;

  &:hover {
    transform: translateY(-1px);
    box-shadow: 0 6px 20px rgba(184, 81, 29, 0.36) !important;
  }

  &:active {
    transform: translateY(0);
  }
}

.hero-hint {
  font-size: var(--text-sm);
  color: var(--color-text-muted);
}

// Hero image side — animate slightly after content
.hero-image-outer {
  position: relative;
  animation: fadeSlideUp var(--duration-slow) var(--ease-out) 120ms both;

  @media (max-width: 960px) {
    display: none;
  }
}

.hero-image-frame {
  border-radius: var(--radius-xl);
  overflow: hidden;
  box-shadow:
    0 24px 48px rgba(0, 0, 0, 0.12),
    0 8px 16px rgba(0, 0, 0, 0.06);
  position: relative;
  z-index: 1;
}

.hero-image {
  width: 100%;
  height: 480px;
  object-fit: cover;
  display: block;
}

// Floating dish badge
.hero-badge {
  position: absolute;
  bottom: -16px;
  left: -24px;
  z-index: 2;
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-md);
  padding: 10px 14px;
  display: flex;
  align-items: center;
  gap: 8px;
  box-shadow: var(--shadow-md);
  font-size: var(--text-sm);
  font-weight: 500;
  color: var(--color-text-primary);
  white-space: nowrap;
  animation: fadeSlideUp var(--duration-slow) var(--ease-out) 280ms both;
}

// Decorative ring behind image
.hero-ring {
  position: absolute;
  top: -20px;
  right: -20px;
  width: 180px;
  height: 180px;
  border-radius: var(--radius-full);
  border: 2px solid var(--color-primary);
  opacity: 0.12;
  z-index: 0;
}

// ── Features ─────────────────────────────────────────────────────────────────

.features-section {
  background-color: var(--color-surface-variant);
  padding: 96px 0;

  @media (max-width: 768px) {
    padding: 64px 0;
  }
}

.features-header {
  text-align: center;
  margin-bottom: var(--space-12);
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: var(--space-3);
}

.features-heading {
  font-family: var(--font-display);
  font-size: clamp(28px, 3.5vw, 40px);
  font-weight: 400;
  line-height: 1.15;
  letter-spacing: -0.02em;
  color: var(--color-text-primary);
  margin: 0;
}

.features-grid {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: var(--space-6);

  @media (max-width: 768px) {
    grid-template-columns: 1fr;
  }
}

// Feature card — hidden initially, revealed by IntersectionObserver
.feature-card {
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-lg);
  padding: var(--space-8) var(--space-6);
  position: relative;
  overflow: hidden;
  opacity: 0;
  transform: translateY(28px);
  transition:
    opacity var(--duration-slow) var(--ease-out) var(--stagger-delay, 0ms),
    transform var(--duration-slow) var(--ease-out) var(--stagger-delay, 0ms);

  .features-grid.is-visible & {
    opacity: 1;
    transform: none;
  }

  &:hover {
    box-shadow: var(--shadow-md);
    transform: translateY(-2px);
    transition:
      opacity var(--duration-slow) var(--ease-out) var(--stagger-delay, 0ms),
      transform var(--duration-fast) var(--ease-out),
      box-shadow var(--duration-fast) var(--ease-out);
  }

  // Override hover transform after reveal
  .features-grid.is-visible &:hover {
    transform: translateY(-2px);
  }
}

.feature-num {
  position: absolute;
  top: -8px;
  right: 16px;
  font-family: var(--font-display);
  font-size: 80px;
  font-weight: 400;
  line-height: 1;
  color: var(--color-text-primary);
  opacity: 0.05;
  user-select: none;
  pointer-events: none;
}

.feature-icon-wrap {
  width: 48px;
  height: 48px;
  border-radius: var(--radius-md);
  background: rgba(212, 101, 42, 0.1);
  display: flex;
  align-items: center;
  justify-content: center;
  margin-bottom: var(--space-4);
}

.feature-icon {
  color: var(--color-primary) !important;
  font-size: 22px !important;
}

.feature-title {
  font-family: var(--font-display);
  font-size: var(--text-xl);
  font-weight: 400;
  line-height: 1.2;
  color: var(--color-text-primary);
  margin: 0 0 var(--space-3);
}

.feature-text {
  font-family: var(--font-body);
  font-size: var(--text-base);
  line-height: 1.65;
  color: var(--color-text-secondary);
  margin: 0;
}

// ── CTA ──────────────────────────────────────────────────────────────────────

.cta-section {
  background-color: var(--color-background);
  padding: 80px 0 96px;

  @media (max-width: 768px) {
    padding: 56px 0 72px;
  }
}

.cta-card {
  background: var(--color-primary-dark);
  border-radius: var(--radius-xl);
  padding: 64px 56px;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: var(--space-8);

  @media (max-width: 768px) {
    flex-direction: column;
    text-align: center;
    padding: 48px 32px;
  }
}

.cta-heading {
  font-family: var(--font-display);
  font-size: clamp(28px, 3vw, 40px);
  font-weight: 400;
  line-height: 1.15;
  color: #fff;
  margin: 0 0 var(--space-3);
}

.cta-sub {
  font-size: var(--text-base);
  line-height: 1.6;
  color: rgba(255, 255, 255, 0.72);
  margin: 0;
  max-width: 400px;
}

.cta-btn {
  background-color: #fff !important;
  color: var(--color-primary-dark) !important;
  border-radius: var(--radius-md) !important;
  font-weight: 700 !important;
  font-size: var(--text-base) !important;
  letter-spacing: 0 !important;
  height: 52px !important;
  padding: 0 28px !important;
  flex-shrink: 0;
  box-shadow: 0 4px 16px rgba(0, 0, 0, 0.16) !important;
  transition: transform var(--duration-fast) var(--ease-out),
              box-shadow var(--duration-fast) var(--ease-out) !important;

  &:hover {
    transform: translateY(-1px);
    box-shadow: 0 8px 24px rgba(0, 0, 0, 0.22) !important;
  }
}

// ── Footer ───────────────────────────────────────────────────────────────────

.landing-footer {
  background-color: #1E120A;
  padding: 32px 0;
}

.footer-inner {
  display: flex;
  align-items: center;
  justify-content: space-between;
  max-width: 1200px;
}

.footer-logo {
  font-family: var(--font-display);
  font-size: 22px;
  letter-spacing: -0.02em;
  line-height: 1;
}

.footer-logo-dinner {
  color: rgba(212, 101, 42, 0.7);
}

.footer-copy {
  font-size: var(--text-sm);
  color: rgba(255, 255, 255, 0.4);
}

// ── Animation Keyframes ───────────────────────────────────────────────────────

@keyframes fadeSlideUp {
  from {
    opacity: 0;
    transform: translateY(20px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}
</style>
