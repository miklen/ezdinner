<script setup lang="ts">
defineProps<{
  weekDishCount: number
  overdueFavoriteName: string | null
  overdueFavoriteDays: number | null
  loading: boolean
}>()
</script>

<template>
  <div class="stats-strip">
    <template v-if="loading">
      <div v-for="i in 2" :key="i" class="stat-skeleton">
        <v-skeleton-loader type="text" height="72" rounded="lg" />
      </div>
    </template>
    <template v-else>
      <div class="stat-card">
        <v-icon size="18" class="stat-card__icon">mdi-silverware-fork-knife</v-icon>
        <span class="stat-card__value">{{ weekDishCount }}</span>
        <span class="stat-card__label">dishes planned this week</span>
      </div>
      <div class="stat-card">
        <v-icon size="18" class="stat-card__icon">mdi-heart</v-icon>
        <span class="stat-card__value stat-card__value--name">{{ overdueFavoriteName ?? '—' }}</span>
        <span class="stat-card__label">{{ overdueFavoriteDays !== null ? `top rated, not had in ${overdueFavoriteDays} days` : 'no rated dishes yet' }}</span>
      </div>
    </template>
  </div>
</template>

<style scoped>
.stats-strip {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  gap: var(--space-3);
}

.stat-skeleton {
  width: 100%;
}

.stat-card {
  display: flex;
  flex-direction: column;
  align-items: flex-start;
  gap: var(--space-1);
  padding: var(--space-4);
  background: var(--color-surface);
  border: 1px solid rgba(0, 0, 0, 0.06);
  border-radius: var(--radius-lg);
  box-shadow: var(--shadow-xs);
  overflow: hidden;
}

.stat-card__icon {
  color: var(--color-primary);
}

.stat-card__value {
  font-family: var(--font-body);
  font-size: var(--text-lg);
  font-weight: 600;
  color: var(--color-text-primary);
  line-height: 1.2;
}

.stat-card__value--name {
  font-size: var(--text-base);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  width: 100%;
}

.stat-card__label {
  font-family: var(--font-body);
  font-size: var(--text-xs);
  color: var(--color-text-muted);
  display: -webkit-box;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
  overflow: hidden;
  width: 100%;
}
</style>
