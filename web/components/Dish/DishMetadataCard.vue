<script setup lang="ts">
import type { Dish, DishMetadata, DishRole, EffortLevel, SeasonAffinity } from '~/types'

const props = defineProps<{
  dish: Dish
  familyId: string
  enriching?: boolean
}>()

const emit = defineEmits<{
  updated: []
}>()

const { dishes: dishRepo } = useRepositories()
const { show: showSnackbar } = useSnackbar()
const { t } = useI18n()

// ── Inline editing state ───────────────────────────────────────────────────

const editingField = shallowRef<'roles' | 'effortLevel' | 'seasonAffinity' | 'cuisine' | null>(null)
const saving = shallowRef(false)

// Edit buffers
const editRoles = ref<DishRole[]>([])
const editEffortLevel = shallowRef<EffortLevel | null>(null)
const editSeasonAffinity = shallowRef<SeasonAffinity | null>(null)
const editCuisine = shallowRef<string>('')

// ── Options ─────────────────────────────────────────────────────────────

const roleOptions: DishRole[] = ['Main', 'Side', 'Dessert', 'Other']
const effortOptions = computed(() => [
  { value: 'Quick' as EffortLevel, label: t('dishes.effortLevels.Quick'), icon: 'mdi-lightning-bolt' },
  { value: 'Medium' as EffortLevel, label: t('dishes.effortLevels.Medium'), icon: 'mdi-clock-outline' },
  { value: 'Elaborate' as EffortLevel, label: t('dishes.effortLevels.Elaborate'), icon: 'mdi-chef-hat' },
])
const seasonOptions = computed(() => [
  { value: 'Summer' as SeasonAffinity, label: t('dishes.seasons.Summer'), icon: 'mdi-weather-sunny' },
  { value: 'Winter' as SeasonAffinity, label: t('dishes.seasons.Winter'), icon: 'mdi-snowflake' },
  { value: 'Spring' as SeasonAffinity, label: t('dishes.seasons.Spring'), icon: 'mdi-flower-outline' },
  { value: 'Autumn' as SeasonAffinity, label: t('dishes.seasons.Autumn'), icon: 'mdi-leaf' },
  { value: 'AllYear' as SeasonAffinity, label: t('dishes.seasons.AllYear'), icon: 'mdi-calendar-blank-outline' },
])

// ── Field display values ─────────────────────────────────────────────────

const displayRoles = computed(() =>
  props.dish.roles?.length
    ? props.dish.roles.map((r) => t(`dishes.roles.${r}`)).join(', ')
    : null,
)
const displayEffort = computed(() =>
  props.dish.effortLevel ? t(`dishes.effortLevels.${props.dish.effortLevel}`) : null,
)
const displaySeason = computed(() => {
  const s = props.dish.seasonAffinity
  if (!s) return null
  return t(`dishes.seasons.${s}`)
})
const displayCuisine = computed(() => props.dish.cuisine ?? null)

const hasAnyMetadata = computed(() =>
  displayRoles.value || displayEffort.value || displaySeason.value || displayCuisine.value,
)

// ── Edit actions ─────────────────────────────────────────────────────────

function startEdit(field: 'roles' | 'effortLevel' | 'seasonAffinity' | 'cuisine') {
  editingField.value = field
  if (field === 'roles') editRoles.value = [...(props.dish.roles ?? [])]
  if (field === 'effortLevel') editEffortLevel.value = props.dish.effortLevel ?? null
  if (field === 'seasonAffinity') editSeasonAffinity.value = props.dish.seasonAffinity ?? null
  if (field === 'cuisine') editCuisine.value = props.dish.cuisine ?? ''
}

function cancelEdit() {
  editingField.value = null
}

async function saveField(field: 'roles' | 'effortLevel' | 'seasonAffinity' | 'cuisine') {
  if (field === 'roles' && editRoles.value.length === 0) {
    showSnackbar(t('dishes.pleaseSelectAtLeastOneRole'), { type: 'error' })
    return
  }
  saving.value = true
  try {
    const body: Record<string, unknown> = {}
    if (field === 'roles') body.roles = editRoles.value
    if (field === 'effortLevel') body.effortLevel = editEffortLevel.value
    if (field === 'seasonAffinity') body.seasonAffinity = editSeasonAffinity.value
    if (field === 'cuisine') body.cuisine = editCuisine.value || null
    await dishRepo.updateMetadata(props.familyId, props.dish.id, body as Partial<DishMetadata>)
    emit('updated')
    cancelEdit()
  } catch {
    showSnackbar(t('dishes.failedToSave'), { type: 'error' })
  } finally {
    saving.value = false
  }
}

// ── Helpers ──────────────────────────────────────────────────────────────

const effortIcon = computed(() => effortOptions.value.find((o) => o.value === props.dish.effortLevel)?.icon ?? 'mdi-clock-outline')
const seasonIcon = computed(() => seasonOptions.value.find((o) => o.value === props.dish.seasonAffinity)?.icon ?? 'mdi-calendar-blank-outline')
</script>

<template>
  <v-card class="metadata-card">
    <!-- Header -->
    <div class="metadata-card__header">
      <span class="text-card-title">{{ $t('dishes.dishMetadata') }}</span>
      <div class="metadata-card__enriching" :style="{ visibility: enriching ? 'visible' : 'hidden' }">
        <v-progress-circular size="12" width="2" indeterminate color="primary" />
        <span>{{ $t('dishes.analyzingDish') }}</span>
      </div>
    </div>

    <div class="metadata-card__body">
      <!-- Roles -->
      <div class="metadata-card__field">
        <div class="metadata-card__field-label">
          <v-icon size="13" class="metadata-card__field-icon">mdi-tag-multiple-outline</v-icon>
          {{ $t('dishes.role') }}
        </div>

        <!-- Edit mode -->
        <div v-if="editingField === 'roles'" class="metadata-card__edit-row">
          <div class="metadata-card__role-checkboxes">
            <label
              v-for="role in roleOptions"
              :key="role"
              class="metadata-card__role-option"
            >
              <input
                v-model="editRoles"
                type="checkbox"
                :value="role"
                class="metadata-card__checkbox"
              />
              {{ $t(`dishes.roles.${role}`) }}
            </label>
          </div>
          <div class="metadata-card__field-actions">
            <v-btn size="x-small" variant="text" :disabled="saving" @click="cancelEdit">{{ $t('common.cancel') }}</v-btn>
            <v-btn size="x-small" variant="text" color="primary" :loading="saving" @click="saveField('roles')">{{ $t('common.save') }}</v-btn>
          </div>
        </div>

        <!-- View mode -->
        <div v-else class="metadata-card__value-row" @click="startEdit('roles')">
          <template v-if="displayRoles">
            <span
              class="metadata-card__value"
              :class="{ 'metadata-card__value--ai': !dish.rolesConfirmed }"
            >
              <v-icon v-if="!dish.rolesConfirmed" size="12" class="metadata-card__ai-icon">mdi-auto-fix</v-icon>
              {{ displayRoles }}
            </span>
          </template>
          <span v-else class="metadata-card__value--empty">{{ $t('dishes.notSet') }}</span>
          <v-icon size="14" class="metadata-card__edit-icon">mdi-pencil-outline</v-icon>
        </div>
      </div>

      <!-- Effort -->
      <div class="metadata-card__field">
        <div class="metadata-card__field-label">
          <v-icon size="13" class="metadata-card__field-icon">mdi-clock-outline</v-icon>
          {{ $t('dishes.effort') }}
        </div>

        <!-- Edit mode -->
        <div v-if="editingField === 'effortLevel'" class="metadata-card__edit-row">
          <div class="metadata-card__option-group" role="radiogroup" aria-label="Effort level">
            <button
              v-for="opt in effortOptions"
              :key="opt.value"
              role="radio"
              :aria-checked="editEffortLevel === opt.value"
              class="metadata-card__option-btn"
              :class="{ 'metadata-card__option-btn--selected': editEffortLevel === opt.value }"
              @click="editEffortLevel = opt.value"
            >
              <v-icon size="14">{{ opt.icon }}</v-icon>
              {{ opt.label }}
            </button>
          </div>
          <div class="metadata-card__field-actions">
            <v-btn size="x-small" variant="text" :disabled="saving" @click="cancelEdit">{{ $t('common.cancel') }}</v-btn>
            <v-btn size="x-small" variant="text" color="primary" :loading="saving" @click="saveField('effortLevel')">{{ $t('common.save') }}</v-btn>
          </div>
        </div>

        <!-- View mode -->
        <div v-else class="metadata-card__value-row" @click="startEdit('effortLevel')">
          <template v-if="displayEffort">
            <span
              class="metadata-card__value"
              :class="{ 'metadata-card__value--ai': !dish.effortLevelConfirmed }"
            >
              <v-icon v-if="!dish.effortLevelConfirmed" size="12" class="metadata-card__ai-icon">mdi-auto-fix</v-icon>
              <v-icon size="14" class="mr-1">{{ effortIcon }}</v-icon>
              {{ displayEffort }}
            </span>
          </template>
          <span v-else class="metadata-card__value--empty">{{ $t('dishes.notSet') }}</span>
          <v-icon size="14" class="metadata-card__edit-icon">mdi-pencil-outline</v-icon>
        </div>
      </div>

      <!-- Season -->
      <div class="metadata-card__field">
        <div class="metadata-card__field-label">
          <v-icon size="13" class="metadata-card__field-icon">mdi-calendar-blank-outline</v-icon>
          {{ $t('dishes.season') }}
        </div>

        <!-- Edit mode -->
        <div v-if="editingField === 'seasonAffinity'" class="metadata-card__edit-row">
          <div class="metadata-card__option-group" role="radiogroup" aria-label="Season affinity">
            <button
              v-for="opt in seasonOptions"
              :key="opt.value"
              role="radio"
              :aria-checked="editSeasonAffinity === opt.value"
              class="metadata-card__option-btn"
              :class="{ 'metadata-card__option-btn--selected': editSeasonAffinity === opt.value }"
              @click="editSeasonAffinity = opt.value"
            >
              <v-icon size="14">{{ opt.icon }}</v-icon>
              {{ opt.label }}
            </button>
          </div>
          <div class="metadata-card__field-actions">
            <v-btn size="x-small" variant="text" :disabled="saving" @click="cancelEdit">{{ $t('common.cancel') }}</v-btn>
            <v-btn size="x-small" variant="text" color="primary" :loading="saving" @click="saveField('seasonAffinity')">{{ $t('common.save') }}</v-btn>
          </div>
        </div>

        <!-- View mode -->
        <div v-else class="metadata-card__value-row" @click="startEdit('seasonAffinity')">
          <template v-if="displaySeason">
            <span
              class="metadata-card__value"
              :class="{ 'metadata-card__value--ai': !dish.seasonAffinityConfirmed }"
            >
              <v-icon v-if="!dish.seasonAffinityConfirmed" size="12" class="metadata-card__ai-icon">mdi-auto-fix</v-icon>
              <v-icon size="14" class="mr-1">{{ seasonIcon }}</v-icon>
              {{ displaySeason }}
            </span>
          </template>
          <span v-else class="metadata-card__value--empty">{{ $t('dishes.notSet') }}</span>
          <v-icon size="14" class="metadata-card__edit-icon">mdi-pencil-outline</v-icon>
        </div>
      </div>

      <!-- Cuisine -->
      <div class="metadata-card__field">
        <div class="metadata-card__field-label">
          <v-icon size="13" class="metadata-card__field-icon">mdi-earth</v-icon>
          {{ $t('dishes.cuisine') }}
        </div>

        <!-- Edit mode -->
        <div v-if="editingField === 'cuisine'" class="metadata-card__edit-row">
          <v-text-field
            v-model="editCuisine"
            :placeholder="$t('dishes.cuisinePlaceholder')"
            aria-label="Cuisine type"
            variant="outlined"
            density="compact"
            hide-details
            autofocus
            class="metadata-card__text-input"
            @keyup.enter="saveField('cuisine')"
            @keyup.esc="cancelEdit"
          />
          <div class="metadata-card__field-actions">
            <v-btn size="x-small" variant="text" :disabled="saving" @click="cancelEdit">{{ $t('common.cancel') }}</v-btn>
            <v-btn size="x-small" variant="text" color="primary" :loading="saving" @click="saveField('cuisine')">{{ $t('common.save') }}</v-btn>
          </div>
        </div>

        <!-- View mode -->
        <div v-else class="metadata-card__value-row" @click="startEdit('cuisine')">
          <template v-if="displayCuisine">
            <span
              class="metadata-card__value"
              :class="{ 'metadata-card__value--ai': !dish.cuisineConfirmed }"
            >
              <v-icon v-if="!dish.cuisineConfirmed" size="12" class="metadata-card__ai-icon">mdi-auto-fix</v-icon>
              {{ displayCuisine }}
            </span>
          </template>
          <span v-else class="metadata-card__value--empty">{{ $t('dishes.notSet') }}</span>
          <v-icon size="14" class="metadata-card__edit-icon">mdi-pencil-outline</v-icon>
        </div>
      </div>

      <!-- AI suggestion legend, shown when any unconfirmed value exists -->
      <div
        v-if="hasAnyMetadata && (!dish.rolesConfirmed || !dish.effortLevelConfirmed || !dish.seasonAffinityConfirmed || !dish.cuisineConfirmed)"
        class="metadata-card__ai-legend"
      >
        <v-icon size="11" class="metadata-card__ai-legend-icon">mdi-auto-fix</v-icon>
        {{ $t('dishes.aiSuggestion') }}
      </div>
    </div>
  </v-card>
</template>

<style scoped>
.metadata-card {
  background-color: var(--color-surface-variant) !important;
}

.metadata-card__header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: var(--space-3) var(--space-3) var(--space-2);
}

.metadata-card__enriching {
  display: flex;
  align-items: center;
  gap: var(--space-2);
  font-size: var(--text-xs);
  color: var(--color-primary);
}

.metadata-card__body {
  padding: 0 var(--space-3) var(--space-3);
  display: flex;
  flex-direction: column;
  gap: var(--space-2);
}

.metadata-card__field {
  display: flex;
  flex-direction: column;
  gap: var(--space-1);
}

.metadata-card__field-label {
  display: flex;
  align-items: center;
  gap: 4px;
  font-size: var(--text-xs);
  font-weight: 600;
  color: var(--color-text-muted);
  text-transform: uppercase;
  letter-spacing: 0.05em;
}

.metadata-card__field-icon {
  color: var(--color-text-muted);
}

.metadata-card__value-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 6px var(--space-2);
  border-radius: var(--radius-sm);
  border: 1px solid transparent;
  cursor: pointer;
  transition:
    background-color var(--duration-fast) var(--ease-out),
    border-color var(--duration-fast) var(--ease-out);
}

.metadata-card__value-row:hover {
  background-color: var(--color-surface);
  border-color: var(--color-border-medium);
}

.metadata-card__value-row:hover .metadata-card__edit-icon {
  opacity: 1;
}

.metadata-card__edit-icon {
  opacity: 0;
  color: var(--color-text-muted);
  transition: opacity var(--duration-fast) var(--ease-out);
  flex-shrink: 0;
}

.metadata-card__value {
  display: flex;
  align-items: center;
  gap: 4px;
  font-size: var(--text-sm);
  color: var(--color-text-primary);
}

.metadata-card__value--ai {
  color: var(--color-text-secondary);
  font-style: italic;
}

.metadata-card__ai-icon {
  color: var(--color-primary);
  flex-shrink: 0;
}

.metadata-card__value--empty {
  font-size: var(--text-sm);
  color: var(--color-text-muted);
  font-style: italic;
}

/* Edit row */
.metadata-card__edit-row {
  display: flex;
  flex-direction: column;
  gap: var(--space-2);
  padding: var(--space-2);
  background-color: var(--color-surface);
  border-radius: var(--radius-sm);
  border: 1px solid var(--color-border-medium);
}

.metadata-card__field-actions {
  display: flex;
  justify-content: flex-end;
  gap: var(--space-1);
}

/* Role checkboxes */
.metadata-card__role-checkboxes {
  display: flex;
  flex-wrap: wrap;
  gap: var(--space-2);
}

.metadata-card__role-option {
  display: flex;
  align-items: center;
  gap: 6px;
  font-size: var(--text-sm);
  cursor: pointer;
  user-select: none;
}

.metadata-card__checkbox {
  width: 14px;
  height: 14px;
  accent-color: var(--color-primary);
  cursor: pointer;
}

/* Option group for effort/season */
.metadata-card__option-group {
  display: flex;
  flex-wrap: wrap;
  gap: var(--space-2);
}

.metadata-card__option-btn {
  display: flex;
  align-items: center;
  gap: 5px;
  padding: 5px var(--space-3);
  border-radius: var(--radius-sm);
  border: 1px solid var(--color-border-medium);
  background: none;
  font-family: var(--font-body);
  font-size: var(--text-sm);
  color: var(--color-text-secondary);
  cursor: pointer;
  transition:
    background-color var(--duration-fast) var(--ease-out),
    border-color var(--duration-fast) var(--ease-out),
    color var(--duration-fast) var(--ease-out);
}

.metadata-card__option-btn:hover {
  background-color: rgba(var(--color-primary-rgb), 0.06);
  border-color: var(--color-primary);
  color: var(--color-primary-dark);
}

.metadata-card__option-btn--selected {
  background-color: rgba(var(--color-primary-rgb), 0.1);
  border-color: var(--color-primary);
  color: var(--color-primary-dark);
  font-weight: 600;
}

.metadata-card__text-input {
  font-size: var(--text-sm);
}

/* AI legend */
.metadata-card__ai-legend {
  display: flex;
  align-items: center;
  gap: 4px;
  font-size: var(--text-xs);
  color: var(--color-text-muted);
  padding-top: var(--space-1);
  border-top: 1px solid var(--color-border-light);
  margin-top: var(--space-1);
}

.metadata-card__ai-legend-icon {
  color: var(--color-primary);
}
</style>
