<script setup lang="ts">
import { computed } from 'vue'
import { DateTime } from 'luxon'
import DishCard from '~/components/Dish/DishCard.vue'
import DishRating from '~/components/Dish/DishRating.vue'
import type { PlanningDish } from '~/types/week-planning'
const props = defineProps<{ entry: PlanningDish; assignedDates: string[]; target: string | null; busy?: boolean }>()
const emit = defineEmits<{ assign: [dishId: string]; otherDate: [dishId: string]; dismiss: [dishId: string] }>()
const { t, locale } = useI18n()
const rating = computed(() => {
  if (props.entry.recommendation) return props.entry.recommendation.rating === null ? null : props.entry.recommendation.rating / 2
  if (props.entry.dish.ratingCount !== undefined) return props.entry.dish.ratingCount > 0 ? props.entry.dish.rating : null
  return props.entry.dish.ratings?.length ? props.entry.dish.rating / 2 : null
})
const lastServed = computed(() => props.entry.recommendation?.lastServed ?? props.entry.stats?.lastUsed?.toFormat('yyyy-MM-dd'))
const datesLabel = computed(() => props.assignedDates.map(date => DateTime.fromISO(date).setLocale(locale.value).toFormat('ccc d/M')).join(', '))
const targetLabel = computed(() => props.target ? DateTime.fromISO(props.target).setLocale(locale.value).toFormat('ccc d/M') : '')
</script>

<template>
  <DishCard class="planning-card" :dish="{ ...entry.dish, rating: rating ?? 0 }" :dish-stats="entry.stats" :clickable="false" :show-management="false">
    <template #name><NuxtLink class="planning-card__name" :to="`/dishes/${entry.dish.id}`">{{ entry.dish.name }}</NuxtLink></template>
    <template #overflow>
      <v-btn v-if="entry.recommendation" icon="mdi-close" variant="text" size="small" :aria-label="t('weekPlanning.dismiss', { name: entry.dish.name })" @click="emit('dismiss', entry.dish.id)" />
    </template>
    <template #rating><DishRating v-if="rating !== null" :model-value="rating" :size="18" :aria-label="t('weekPlanning.ratingValue', { rating })" /></template>
    <template #stat>
    <span class="planning-card__signals">
      <span v-if="lastServed">{{ t('weekPlanning.lastServed', { date: DateTime.fromISO(lastServed).setLocale(locale).toFormat('d MMM yyyy') }) }}</span>
      <span v-else>{{ t(entry.stats?.timesUsed === 0 || entry.recommendation?.neverUsed ? 'weekPlanning.neverUsed' : 'weekPlanning.historyUnavailable') }}</span>
      <span v-if="!entry.dish.roles?.length">{{ t('weekPlanning.unclassified') }}</span>
      <span v-if="entry.wishVotes !== undefined">{{ t('weekPlanning.wishVotes', { count: entry.wishVotes }) }}</span>
    </span>
    </template>
    <template #additional>
    <div v-if="entry.recommendation" class="planning-card__reasons">
      <p v-for="(reason, index) in entry.recommendation.historicalReasons" :key="`history-${index}`">{{ t(`weekPlanning.reasons.${reason.kind}`, { value: reason.value }) }}</p>
      <p v-for="(reason, index) in entry.recommendation.explanations" :key="`explanation-${index}`">{{ reason.text }} <small>{{ t(`weekPlanning.evidence.${reason.kind}`) }}</small></p>
      <p v-for="limitation in entry.recommendation.limitations" :key="limitation" class="planning-card__limitation">{{ limitation }}</p>
    </div>
    <p v-if="assignedDates.length" class="planning-card__assigned">{{ t('weekPlanning.assignedDates', { dates: datesLabel }) }}</p>
    </template>
    <template #actions>
    <div class="planning-card__actions">
      <v-btn color="primary" variant="tonal" rounded="lg" size="small" prepend-icon="mdi-calendar-plus" :disabled="busy" @click="emit('assign', entry.dish.id)">{{ target ? t('weekPlanning.addToDay', { day: targetLabel }) : t('weekPlanning.planDish') }}</v-btn>
      <v-btn v-if="target" variant="text" :disabled="busy" @click="emit('otherDate', entry.dish.id)">{{ t('weekPlanning.otherDay') }}</v-btn>
    </div>
    </template>
  </DishCard>
</template>

<style scoped>
.planning-card { min-width: 0; }
.planning-card__name { color: inherit; text-decoration: none; }
.planning-card__name:hover { text-decoration: underline; text-underline-offset: 3px; }
.planning-card__signals { display: flex; flex-wrap: wrap; gap: var(--space-2); color: var(--color-text-secondary); font-size: var(--text-xs); margin: 0; }
.planning-card__reasons { font-size: var(--text-sm); }
.planning-card__reasons p { margin: 0; }
.planning-card__limitation { color: var(--color-text-muted); }
.planning-card__assigned { font-size: var(--text-sm); color: var(--color-primary-dark); margin-top: var(--space-3); }
.planning-card__actions { display: flex; flex-wrap: wrap; gap: var(--space-2); }
</style>
