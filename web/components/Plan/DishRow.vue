<script setup lang="ts">
import { DateTime } from 'luxon'
import type { Dish } from '~/types'

const props = defineProps<{ dish: Dish }>()

const wishlistStore = useWishlistStore()
const { t } = useI18n()

const wish = computed(() => wishlistStore.wishes.find((w) => w.dishId === props.dish.id) ?? null)

const daysSince = computed(() => {
  const lastUsed = props.dish.dishStats?.lastUsed
  if (!lastUsed) return null
  const days = Math.round(
    DateTime.now()
      .diff(DateTime.fromISO(lastUsed as unknown as string), 'days')
      .days,
  )
  return days >= 0 ? days : null
})

const rating = computed(() =>
  props.dish.rating > 0 ? props.dish.rating.toFixed(1) : '—',
)
</script>

<template>
  <div class="dish-row__inner">
    <span class="dish-row__name">{{ dish.name }}</span>

    <span v-if="wish" class="dish-row__wish">
      <v-icon size="11" icon="mdi-star" />
      {{ wish.voteCount }}
    </span>

    <span class="dish-row__rating">
      <v-icon size="12" color="#C05040">mdi-heart</v-icon>
      {{ rating }}
    </span>

    <span v-if="daysSince !== null" class="dish-row__days">
      {{ t('plan.dAgo', { days: daysSince }) }}
    </span>
  </div>
</template>

<style scoped>
.dish-row__inner {
  display: flex;
  align-items: center;
  gap: var(--space-2);
  width: 100%;
  min-height: 0;
  padding: 0;
}

.dish-row__name {
  flex: 1;
  font-size: var(--text-sm);
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.dish-row__wish {
  display: flex;
  align-items: center;
  gap: 2px;
  font-size: var(--text-xs);
  font-weight: 600;
  color: var(--color-primary);
  white-space: nowrap;
  flex-shrink: 0;
  background: rgba(var(--color-primary-rgb), 0.1);
  border-radius: var(--radius-full);
  padding: 1px 5px 1px 4px;
}

.dish-row__rating {
  display: flex;
  align-items: center;
  gap: 3px;
  font-size: var(--text-xs);
  color: var(--color-text-muted);
  white-space: nowrap;
  flex-shrink: 0;
}

.dish-row__days {
  font-size: var(--text-xs);
  color: var(--color-text-muted);
  white-space: nowrap;
  flex-shrink: 0;
}
</style>
