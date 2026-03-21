<script setup lang="ts">
import { DateTime } from 'luxon'
import type { Dish, DishStats } from '~/types'

defineOptions({ inheritAttrs: false })

const props = withDefaults(defineProps<{
  dish: Dish
  dishStats?: DishStats
  clickable?: boolean
}>(), {
  dishStats: undefined,
  clickable: true,
})

const emit = defineEmits<{
  'menuitem:moved': []
  archived: []
}>()

const appStore = useAppStore()
const dishesStore = useDishesStore()
const { dishes: dishRepo, dinners: dinnerRepo } = useRepositories()

const confirmDialog = shallowRef(false)
const moveDialog = shallowRef(false)
const editNameMode = shallowRef(false)
const newName = shallowRef('')
const name = shallowRef(props.dish.name)
const moveToDish = shallowRef<Dish | null>(null)

const effectiveStats = computed<DishStats>(() =>
  props.dishStats ?? { dishId: props.dish.id, lastUsed: undefined, timesUsed: 0 },
)

const otherDishes = computed(() =>
  dishesStore.dishes
    .filter((d) => d.id !== props.dish.id)
    .sort((a, b) => a.name.localeCompare(b.name)),
)

const daysAgo = computed(() =>
  effectiveStats.value.lastUsed
    ? Math.floor(DateTime.now().diff(effectiveStats.value.lastUsed, 'days').days)
    : 0,
)

const statCaption = computed(() => {
  const n = effectiveStats.value.timesUsed
  if (n === 0) return 'Never planned for dinner'
  const times = `${n} time${n === 1 ? '' : 's'}`
  if (!effectiveStats.value.lastUsed) return `Had ${times}`
  return `Had ${times} · last ${daysAgo.value}d ago`
})

// Left border accent color by rating tier
const ROLE_ICONS: Record<string, string> = {
  Main: 'mdi-silverware-fork-knife',
  Side: 'mdi-food-variant',
  Dessert: 'mdi-cake-variant-outline',
  Other: 'mdi-dots-horizontal',
}
const EFFORT_ICONS: Record<string, string> = {
  Quick: 'mdi-lightning-bolt',
  Medium: 'mdi-clock-outline',
  Elaborate: 'mdi-chef-hat',
}
const SEASON_DATA: Record<string, { icon: string; label: string }> = {
  Summer: { icon: 'mdi-weather-sunny', label: 'Summer' },
  Winter: { icon: 'mdi-snowflake', label: 'Winter' },
  Spring: { icon: 'mdi-flower-outline', label: 'Spring' },
  Autumn: { icon: 'mdi-leaf', label: 'Autumn' },
  AllYear: { icon: 'mdi-calendar-blank-outline', label: 'All year' },
}

const metadataTags = computed(() => {
  const tags: { icon: string; label: string }[] = []
  for (const role of props.dish.roles ?? []) {
    tags.push({ icon: ROLE_ICONS[role] ?? 'mdi-silverware-fork-knife', label: role })
  }
  if (props.dish.effortLevel) {
    tags.push({ icon: EFFORT_ICONS[props.dish.effortLevel] ?? 'mdi-clock-outline', label: props.dish.effortLevel })
  }
  if (props.dish.seasonAffinity) {
    const s = SEASON_DATA[props.dish.seasonAffinity]
    if (s) tags.push(s)
  }
  if (props.dish.cuisine) {
    tags.push({ icon: 'mdi-earth', label: props.dish.cuisine })
  }
  return tags
})

const accentColor = computed(() => {
  const r = props.dish.rating
  if (r >= 4) return 'var(--color-accent)'
  if (r >= 2.5) return 'var(--color-primary)'
  return 'var(--color-border-medium)'
})

watch(
  () => props.dish.name,
  (val) => { name.value = val },
)

function enableEditNameMode() {
  newName.value = name.value
  editNameMode.value = true
}

async function doUpdateName() {
  name.value = newName.value
  editNameMode.value = false
  try {
    await dishRepo.updateName(props.dish.id, newName.value)
    await dishesStore.updateDish(props.dish.id)
  } catch {
    name.value = props.dish.name
  }
}

async function doDelete() {
  await dishRepo.delete(appStore.activeFamilyId, props.dish.id)
  confirmDialog.value = false
  await dishesStore.populateDishes()
}

async function doMove() {
  if (!moveToDish.value) return
  await dinnerRepo.moveDinnerDishes(appStore.activeFamilyId, props.dish.id, moveToDish.value.id)
  moveDialog.value = false
  emit('menuitem:moved')
}
</script>

<template>
  <!-- Card — full height so sibling cards in the same row align -->
  <div
    class="dish-card-wrap"
    v-bind="$attrs"
    :style="{ '--accent': accentColor }"
  >
    <v-card
      class="dish-card"
      :class="{ 'dish-card--clickable': clickable }"
      elevation="0"
      :ripple="clickable && !editNameMode"
      @click="clickable && !editNameMode && navigateTo(`/dishes/${dish.id}`)"
    >
      <!-- Rating-tier accent bar on the left edge -->
      <div class="dish-card__accent" />

      <div class="dish-card__body">
        <!-- Overflow menu: top-right, revealed on card hover -->
        <div class="dish-card__overflow">
          <DishOverflowMenu
            :dish="dish"
            @edit-name="enableEditNameMode"
            @move="moveDialog = true"
            @delete="confirmDialog = true"
            @archived="emit('archived')"
          />
        </div>

        <!-- Name: display mode -->
        <div
          v-if="!editNameMode"
          class="dish-card__name text-card-title"
        >
          {{ name }}
          <span v-if="dish.isArchived" class="dish-card__archived-label">archived</span>
        </div>

        <!-- Name: inline edit mode -->
        <div
          v-else
          class="dish-card__edit"
          @click.stop
        >
          <v-text-field
            v-model="newName"
            autofocus
            density="compact"
            variant="outlined"
            hide-details
            @keyup.enter="doUpdateName"
            @keyup.esc="editNameMode = false"
            @click.stop
          />
          <div class="dish-card__edit-actions">
            <v-btn
              icon="mdi-check"
              size="small"
              variant="text"
              color="primary"
              @click.stop="doUpdateName"
            />
            <v-btn
              icon="mdi-close"
              size="small"
              variant="text"
              @click.stop="editNameMode = false"
            />
          </div>
        </div>

        <!-- Rating -->
        <div class="dish-card__rating">
          <DishRating :model-value="dish.rating" :size="18" />
        </div>

        <!-- Stat caption -->
        <p class="dish-card__stat text-caption-label">
          {{ statCaption }}
        </p>

        <!-- Metadata tags -->
        <div v-if="metadataTags.length" class="dish-card__tags">
          <span
            v-for="tag in metadataTags"
            :key="tag.label"
            class="meta-tag"
          ><v-icon :icon="tag.icon" size="9" />{{ tag.label }}</span>
        </div>
      </div>
    </v-card>
  </div>

  <!-- Delete confirmation -->
  <v-dialog v-model="confirmDialog" width="400">
    <v-card>
      <v-card-title>Delete dish?</v-card-title>
      <v-card-text>
        Are you sure you want to delete <strong>{{ dish.name }}</strong>?
        This cannot be undone.
      </v-card-text>
      <v-card-actions>
        <v-spacer />
        <v-btn variant="text" @click="confirmDialog = false">Cancel</v-btn>
        <v-btn variant="text" color="error" @click="doDelete">Delete</v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>

  <!-- Move occurrences -->
  <v-dialog v-model="moveDialog" width="440">
    <v-card>
      <v-card-title>Move occurrences</v-card-title>
      <v-card-text>
        Move all {{ effectiveStats.timesUsed }}
        dinner occurrence{{ effectiveStats.timesUsed === 1 ? '' : 's' }} of
        <strong>{{ dish.name }}</strong> to:
        <v-autocomplete
          v-model="moveToDish"
          :items="otherDishes"
          item-title="name"
          return-object
          density="compact"
          variant="outlined"
          placeholder="Select a dish…"
          class="mt-3"
        />
      </v-card-text>
      <v-card-actions>
        <v-spacer />
        <v-btn variant="text" @click="moveDialog = false">Cancel</v-btn>
        <v-btn
          variant="text"
          color="primary"
          :disabled="!moveToDish"
          @click="doMove"
        >Move</v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>
</template>

<style scoped>
.dish-card-wrap {
  /* grid stretches this to row height automatically */
}

.dish-card {
  height: 100%;
  position: relative;
  overflow: hidden;
  transition:
    transform var(--duration-fast) var(--ease-out),
    box-shadow var(--duration-fast) var(--ease-out);
  text-decoration: none;
}

/* Reveal overflow button on card hover/focus-within */
.dish-card:hover :deep(.overflow-btn),
.dish-card:focus-within :deep(.overflow-btn) {
  opacity: 1;
}

@media (hover: hover) {
  .dish-card--clickable:hover {
    transform: translateY(-2px);
    box-shadow: var(--shadow-md) !important;
    cursor: pointer;
  }
}

/* Rating-tier accent bar */
.dish-card__accent {
  position: absolute;
  left: 0;
  top: 0;
  bottom: 0;
  width: 4px;
  background-color: var(--accent);
  border-radius: var(--radius-lg) 0 0 var(--radius-lg);
  transition: background-color var(--duration-fast) var(--ease-out);
}

.dish-card__body {
  padding: var(--space-3) var(--space-3) var(--space-3) calc(var(--space-3) + 8px);
  position: relative;
}

/* Overflow menu pinned top-right */
.dish-card__overflow {
  position: absolute;
  top: var(--space-1);
  right: var(--space-1);
}

/* Dish name — leave room for overflow button */
.dish-card__name {
  padding-right: 32px;
  margin-bottom: var(--space-2);
  word-break: break-word;
}

.dish-card__edit {
  padding-right: 8px;
  margin-bottom: var(--space-2);
}

.dish-card__edit-actions {
  display: flex;
  gap: 2px;
  margin-top: var(--space-1);
}

.dish-card__rating {
  margin-bottom: var(--space-2);
}

.dish-card__stat {
  margin: 0;
}

.dish-card__tags {
  display: flex;
  flex-wrap: wrap;
  gap: 4px;
  margin-top: var(--space-2);
}

.meta-tag {
  display: inline-flex;
  align-items: center;
  gap: 3px;
  padding: 2px 6px;
  border-radius: 3px;
  background: rgba(0, 0, 0, 0.05);
  font-size: 10px;
  font-weight: 700;
  letter-spacing: 0.05em;
  text-transform: uppercase;
  color: var(--color-text-muted);
  line-height: 1.5;
}

.dish-card__archived-label {
  display: inline-block;
  vertical-align: middle;
  margin-left: var(--space-2);
  font-size: 10px;
  font-weight: 600;
  letter-spacing: 0.1em;
  text-transform: uppercase;
  color: var(--color-text-muted);
  border: 1px dashed var(--color-border-medium);
  border-radius: var(--radius-sm);
  padding: 1px 5px;
  line-height: 1.6;
}
</style>
