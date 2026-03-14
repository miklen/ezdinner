<script setup lang="ts">
import type { Dinner } from '~/types'

const props = defineProps<{
  dinner: Dinner | null
  loading: boolean
  firstName?: string
}>()

const greeting = computed(() => {
  const hour = new Date().getHours()
  const name = props.firstName ? `, ${props.firstName}` : ''
  if (hour < 12) return `Good morning${name}`
  if (hour < 17) return `Good afternoon${name}`
  return `Good evening${name}`
})

const todayLabel = computed(() =>
  new Date().toLocaleDateString('en-US', { weekday: 'long', month: 'long', day: 'numeric' }),
)
</script>

<template>
  <div class="hero-section">
    <div class="hero-greeting">
      <h1 class="text-page-title">{{ greeting }}</h1>
      <span class="hero-date">{{ todayLabel }}</span>
    </div>

    <v-skeleton-loader v-if="loading" type="card" height="180" rounded="lg" />

    <div
      v-else
      class="hero-card"
      role="link"
      tabindex="0"
      @click="navigateTo('/plan')"
      @keydown.enter="navigateTo('/plan')"
      @keydown.space.prevent="navigateTo('/plan')"
    >
      <div class="hero-card__eyebrow">TONIGHT</div>
      <template v-if="dinner && dinner.menu.length">
        <div class="hero-card__menu">
          <DishPill v-for="item in dinner.menu" :key="item.dishId" :name="item.dishName" :to="`/dishes/${item.dishId}`" size="md" />
        </div>
        <span class="hero-card__hint">View plan</span>
      </template>
      <div v-else @click.stop>
        <EmptyState
          icon="mdi-silverware-fork-knife"
          message="Nothing planned for tonight"
          action-label="Plan tonight"
          action-to="/plan"
        />
      </div>
    </div>
  </div>
</template>

<style scoped>
.hero-section {
  display: flex;
  flex-direction: column;
  gap: var(--space-4);
}

.hero-greeting {
  display: flex;
  flex-direction: column;
  gap: var(--space-1);
}

.hero-date {
  font-family: var(--font-body);
  font-size: var(--text-sm);
  color: var(--color-text-muted);
  letter-spacing: 0.01em;
}

.hero-card {
  background: linear-gradient(150deg, var(--color-primary-light) 0%, var(--color-surface) 65%);
  border: 1px solid rgba(0, 0, 0, 0.06);
  border-radius: var(--radius-xl);
  padding: var(--space-6);
  cursor: pointer;
  transition: box-shadow var(--duration-fast) var(--ease-out);
  box-shadow: var(--shadow-sm);
  min-height: 140px;
  display: flex;
  flex-direction: column;
  gap: var(--space-4);
  outline-offset: 2px;
}

.hero-card:hover {
  box-shadow: var(--shadow-md);
}

.hero-card:focus-visible {
  outline: 2px solid var(--color-primary);
}

.hero-card__eyebrow {
  font-family: var(--font-body);
  font-size: var(--text-xs);
  font-weight: 600;
  letter-spacing: 0.08em;
  color: var(--color-primary-dark);
}

.hero-card__menu {
  display: flex;
  flex-wrap: wrap;
  gap: var(--space-2);
}

.hero-card__hint {
  font-family: var(--font-body);
  font-size: var(--text-xs);
  color: var(--color-text-muted);
  margin-top: auto;
}
</style>
