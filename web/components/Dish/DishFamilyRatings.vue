<script setup lang="ts">
import type { Dish, FamilyMember } from '~/types'
import { nextTick, onScopeDispose, shallowRef, watch } from 'vue'
import type { ComponentPublicInstance } from 'vue'

const props = defineProps<{
  dish?: Dish
  familyMembers: FamilyMember[]
  userId: string
  loading?: boolean
  focusKey?: string
}>()

const emit = defineEmits<{
  updated: []
}>()

const { dishes: dishRepo } = useRepositories()
const { show: showSnackbar } = useSnackbar()
const { t } = useI18n()
const pending = shallowRef(false)
const ownRow = shallowRef<HTMLElement | null>(null)
const highlighted = shallowRef(false)
let focusedKey = ''
let highlightTimer: ReturnType<typeof setTimeout> | undefined
function setOwnRow(element: Element | ComponentPublicInstance | null) { ownRow.value = element instanceof HTMLElement ? element : null }
watch([() => props.focusKey, () => props.dish?.id, () => props.userId, () => props.loading, ownRow], async () => {
  if (!props.focusKey || props.loading || !props.dish || !ownRow.value || focusedKey === props.focusKey) return
  const key = props.focusKey
  await nextTick()
  if (key !== props.focusKey || !ownRow.value) return
  focusedKey = key
  ownRow.value.scrollIntoView({ block: 'center', behavior: 'instant' })
  ownRow.value.focus({ preventScroll: true })
  highlighted.value = true
  clearTimeout(highlightTimer)
  highlightTimer = setTimeout(() => { highlighted.value = false }, 2500)
}, { flush: 'post' })
onScopeDispose(() => { clearTimeout(highlightTimer) })

const ratings = computed<Record<string, number>>(() =>
  Object.fromEntries(props.dish?.ratings?.map((r) => [r.familyMemberId, r.rating]) ?? []),
)

const averageRating = computed(() => {
  const all = props.dish?.ratings?.map((r) => r.rating) ?? []
  if (!all.length) return null
  return all.reduce((sum, r) => sum + r, 0) / all.length
})

function getInitials(name: string): string {
  const parts = name.trim().split(' ')
  if (parts.length === 0) return '?'
  if (parts.length === 1) return parts[0][0].toUpperCase()
  return parts[0][0].toUpperCase() + parts[parts.length - 1][0].toUpperCase()
}

async function updateRating(val: number, familyMemberId: string) {
  if (pending.value || !props.dish) return
  const member = props.familyMembers.find(candidate => candidate.id === familyMemberId)
  if (!member || (member.hasAutonomy && member.id !== props.userId)) return
  pending.value = true
  try {
    await dishRepo.updateRating(props.dish.id, val, familyMemberId)
    showSnackbar(t('ratingReminders.rated'), { type: 'success' })
    emit('updated')
  } catch {
    showSnackbar(t('ratingReminders.rateFailed'), { type: 'error' })
  } finally {
    pending.value = false
  }
}
</script>

<template>
  <v-card class="ratings-card">
    <!-- Skeleton -->
    <template v-if="loading">
      <v-card-text>
        <v-skeleton-loader
          v-for="i in 3"
          :key="i"
          type="list-item-avatar"
          class="mb-1"
        />
      </v-card-text>
    </template>

    <!-- Loaded -->
    <template v-else>
      <div class="ratings-card__header">
        <span class="text-card-title">{{ $t('dishes.familyRatings') }}</span>
        <div v-if="averageRating !== null" class="ratings-card__avg">
          <v-icon size="13" :color="'#C05040'">mdi-heart</v-icon>
          <span class="ratings-card__avg-value">{{ t('ratingReminders.average', { value: averageRating.toFixed(1) }) }}</span>
        </div>
      </div>

      <div class="ratings-card__rows">
        <div
          v-for="member in familyMembers"
          :id="member.id === userId ? 'my-rating' : undefined"
          :key="member.id"
          :ref="member.id === userId ? setOwnRow : undefined"
          :tabindex="member.id === userId ? -1 : undefined"
          class="ratings-card__row"
          :class="{ 'ratings-card__row--current': member.id === userId, 'ratings-card__row--highlighted': member.id === userId && highlighted }"
        >
          <!-- Avatar -->
          <v-avatar
            :color="member.id === userId ? 'primary' : undefined"
            size="34"
            class="ratings-card__avatar"
          >
            <span
              class="ratings-card__initials"
              :style="{ color: member.id === userId ? '#fff' : 'var(--color-text-secondary)' }"
            >
              {{ getInitials(member.name) }}
            </span>
          </v-avatar>

          <!-- Name + You badge -->
          <div class="ratings-card__member">
            <span class="ratings-card__name">{{ member.name }}</span>
            <span v-if="member.id === userId" class="ratings-card__you-badge">{{ $t('dishes.youBadge') }}</span>
          </div>

          <!-- Rating hearts -->
          <div class="ratings-card__stars">
            <DishRating
              :gap="2"
              :model-value="ratings[member.id] ?? 0"
              :editable="!member.hasAutonomy || member.id === userId"
              :disabled="pending"
              :label="t('ratingReminders.memberRating', { name: member.name })"
              @update:model-value="(val) => updateRating(val, member.id)"
            />
          </div>
        </div>
      </div>
    </template>
  </v-card>
</template>

<style scoped>
.ratings-card__header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: var(--space-3) var(--space-3) var(--space-2);
}

.ratings-card__avg {
  display: flex;
  align-items: center;
  gap: 4px;
}

.ratings-card__avg-value {
  font-size: var(--text-xs);
  font-weight: 600;
  color: var(--color-text-muted);
  letter-spacing: 0.02em;
}

.ratings-card__rows {
  padding: 0 var(--space-2) var(--space-2);
  display: flex;
  flex-direction: column;
  gap: var(--space-1);
}

.ratings-card__row {
  display: flex;
  align-items: center;
  gap: var(--space-3);
  padding: var(--space-2) var(--space-2);
  border-radius: var(--radius-md);
  transition: background-color var(--duration-fast) var(--ease-out);
}

.ratings-card__row--current {
  background-color: var(--color-surface-variant);
}
.ratings-card__row--highlighted { outline: 2px solid var(--color-primary); outline-offset: 2px; }

.ratings-card__avatar {
  flex-shrink: 0;
}

.ratings-card__initials {
  font-size: var(--text-xs);
  font-weight: 700;
  line-height: 1;
}

.ratings-card__member {
  flex: 1;
  min-width: 0;
  display: flex;
  flex-direction: column;
  align-items: flex-start;
  gap: 2px;
}

.ratings-card__name {
  width: 100%;
  font-size: var(--text-sm);
  font-weight: 500;
  color: var(--color-text-primary);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.ratings-card__you-badge {
  flex-shrink: 0;
  font-size: 10px;
  font-weight: 700;
  letter-spacing: 0.06em;
  text-transform: uppercase;
  color: var(--color-primary-dark);
  background-color: rgba(var(--color-primary-rgb), 0.12);
  padding: 1px 6px;
  border-radius: var(--radius-full);
}

.ratings-card__stars {
  flex-shrink: 0;
}

.ratings-card__stars :deep(.v-btn) {
  min-width: unset !important;
  width: auto !important;
  padding: 0 !important;
}
</style>
