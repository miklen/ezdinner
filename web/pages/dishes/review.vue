<script setup lang="ts">
import type { Dish } from '~/types'

useHead({ title: 'Review AI suggestions' })

const appStore = useAppStore()
const { dishes: dishRepo } = useRepositories()

const dishes = ref<Dish[]>([])
const loading = shallowRef(true)

const pendingDishes = computed(() =>
  dishes.value.filter((d) =>
    (!d.rolesConfirmed && d.roles?.length) ||
    (!d.effortLevelConfirmed && d.effortLevel) ||
    (!d.seasonAffinityConfirmed && d.seasonAffinity) ||
    (!d.cuisineConfirmed && d.cuisine),
  ),
)

async function load() {
  loading.value = true
  try {
    dishes.value = await dishRepo.all(appStore.activeFamilyId)
  } finally {
    loading.value = false
  }
}

async function refreshDish(dishId: string) {
  const updated = await dishRepo.get(dishId)
  const idx = dishes.value.findIndex((d) => d.id === dishId)
  if (idx !== -1) dishes.value[idx] = updated
}

onMounted(load)
watch(() => appStore.activeFamilyId, () => navigateTo('/dishes'))
</script>

<template>
  <div class="review-page">
    <!-- Header -->
    <div class="review-page__header">
      <nav class="review-page__breadcrumb text-caption-label" aria-label="Breadcrumb">
        <NuxtLink to="/dishes" class="review-page__breadcrumb-link">
          <v-icon size="13">mdi-silverware-fork-knife</v-icon>
          Dishes
        </NuxtLink>
        <span class="review-page__breadcrumb-sep" aria-hidden="true">/</span>
        <span>Review AI suggestions</span>
      </nav>

      <h1 class="text-page-title review-page__title">Review AI suggestions</h1>
      <p class="review-page__subtitle">
        Fields below were suggested by AI. Click a field to confirm or adjust it.
      </p>
    </div>

    <!-- Loading skeleton -->
    <div v-if="loading" class="review-page__list" role="status" aria-live="polite" aria-busy="true">
      <v-skeleton-loader v-for="i in 3" :key="i" type="card" class="review-page__skeleton" />
    </div>

    <!-- Empty state -->
    <EmptyState
      v-else-if="!pendingDishes.length"
      icon="mdi-check-circle-outline"
      message="All dish metadata has been confirmed — nothing to review."
    />

    <!-- Pending dishes -->
    <div v-else class="review-page__list">
      <div
        v-for="dish in pendingDishes"
        :key="dish.id"
        class="review-page__item"
      >
        <NuxtLink :to="`/dishes/${dish.id}`" class="review-page__dish-link">
          {{ dish.name }}
        </NuxtLink>
        <DishMetadataCard
          :dish="dish"
          :family-id="appStore.activeFamilyId"
          @updated="refreshDish(dish.id)"
        />
      </div>
    </div>
  </div>
</template>

<style scoped>
.review-page {
  padding: var(--space-6) var(--space-4);
  max-width: 720px;
}

.review-page__header {
  margin-bottom: var(--space-6);
}

.review-page__breadcrumb {
  display: flex;
  align-items: center;
  gap: var(--space-2);
  margin-bottom: var(--space-3);
  color: var(--color-text-muted);
}

.review-page__breadcrumb-link {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  color: var(--color-text-muted);
  text-decoration: none;
  transition: color var(--duration-fast) var(--ease-out);
}

.review-page__breadcrumb-link:hover {
  color: var(--color-primary-dark);
}

.review-page__breadcrumb-sep {
  color: var(--color-border-medium);
}

.review-page__title {
  margin: 0 0 var(--space-2);
}

.review-page__subtitle {
  margin: 0;
  font-size: var(--text-sm);
  color: var(--color-text-secondary);
}

.review-page__list {
  display: flex;
  flex-direction: column;
  gap: var(--space-4);
}

.review-page__skeleton {
  border-radius: var(--radius-md);
}

.review-page__item {
  display: flex;
  flex-direction: column;
  gap: var(--space-2);
}

.review-page__dish-link {
  font-family: var(--font-display);
  font-size: var(--text-base);
  font-weight: 600;
  color: var(--color-primary-dark);
  text-decoration: none;
  transition: color var(--duration-fast) var(--ease-out);
}

.review-page__dish-link:hover {
  color: var(--color-primary);
}
</style>
