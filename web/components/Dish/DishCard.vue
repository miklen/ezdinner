<template>
  <span>
    <v-card rounded="lg">
      <v-card-title v-show="!editNameMode" @click="navigateTo('/dishes/' + dish.id)">
        <v-row style="overflow: hidden">
          <v-col style="word-break: normal; cursor: pointer">{{ name }}</v-col>
          <v-col class="text-right" cols="4" sm="3" lg="5" xl="3">
            <v-icon @click.stop="enableEditNameMode()">mdi-pencil</v-icon>
            <v-icon @click.stop="moveDialog = true">mdi-transfer-right</v-icon>
            <v-icon @click.stop="confirmDialog = true">mdi-trash-can</v-icon>
          </v-col>
        </v-row>
      </v-card-title>

      <v-card-title v-show="editNameMode">
        <v-row>
          <v-col>
            <v-text-field
              v-model="newName"
              autofocus
              density="compact"
              @keyup.enter="doUpdateName()"
              @keyup.esc="editNameMode = false"
            />
          </v-col>
          <v-col cols="2">
            <v-icon @click.stop="doUpdateName()">mdi-check</v-icon>
            <v-icon @click.stop="editNameMode = false">mdi-close</v-icon>
          </v-col>
        </v-row>
      </v-card-title>

      <v-card-subtitle>
        <v-rating
          color="primary"
          half-increments
          empty-icon="mdi-heart-outline"
          full-icon="mdi-heart"
          half-icon="mdi-heart-half-full"
          length="5"
          size="20"
          :model-value="dish.rating"
          readonly
        />
      </v-card-subtitle>

      <v-card-text>
        <v-row>
          <v-col v-if="effectiveStats.timesUsed > 0">
            You've had {{ name }} for dinner {{ timesUsedText }}
          </v-col>
        </v-row>
        <v-row>
          <v-col v-if="effectiveStats.lastUsed">
            Last was {{ daysAgo }} days ago on {{ lastUsedFormatted }}
          </v-col>
          <v-col v-else>Never been planned for dinner</v-col>
        </v-row>
      </v-card-text>
    </v-card>

    <v-dialog v-model="confirmDialog" width="400">
      <v-card>
        <v-card-title>Delete?</v-card-title>
        <v-card-text>Are you sure you want to delete <strong>{{ dish.name }}</strong>?</v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn variant="text" @click="confirmDialog = false">Cancel</v-btn>
          <v-btn variant="text" color="error" @click="doDelete()">Delete</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <v-dialog v-model="moveDialog" width="400">
      <v-card>
        <v-card-title>Move {{ effectiveStats.timesUsed }} dinner occurrences?</v-card-title>
        <v-card-text>
          Move all tracked dinner occurrences of {{ dish.name }} to be tracked as
          <v-autocomplete
            v-model="moveToDish"
            :items="otherDishes"
            item-title="name"
            return-object
            style="width: 50%; display: inline-block; margin-left: 10px"
          />
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn variant="text" @click="moveDialog = false">Cancel</v-btn>
          <v-btn variant="text" color="error" @click="doMove()">Move</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>
  </span>
</template>

<script setup lang="ts">
import { DateTime } from 'luxon'
import type { Dish, DishStats } from '~/types'

const props = defineProps<{
  dish: Dish
  dishStats?: DishStats
}>()

const emit = defineEmits<{
  'menuitem:moved': []
}>()

const appStore = useAppStore()
const dishesStore = useDishesStore()
const { dishes: dishRepo, dinners: dinnerRepo } = useRepositories()

const confirmDialog = ref(false)
const moveDialog = ref(false)
const editNameMode = ref(false)
const newName = ref('')
const name = ref(props.dish.name)
const moveToDish = ref<Dish | null>(null)

const effectiveStats = computed<DishStats>(() =>
  props.dishStats ?? { dishId: props.dish.id, lastUsed: undefined, timesUsed: 0 },
)

const otherDishes = computed(() =>
  dishesStore.dishes.filter((d) => d.id !== props.dish.id).sort((a, b) => a.name.localeCompare(b.name)),
)

const lastUsedFormatted = computed(() =>
  effectiveStats.value.lastUsed?.toLocaleString(DateTime.DATE_FULL) ?? 'Never used',
)

const daysAgo = computed(() =>
  effectiveStats.value.lastUsed
    ? Math.floor(DateTime.now().diff(effectiveStats.value.lastUsed, 'days').days)
    : 0,
)

const timesUsedText = computed(() => {
  const n = effectiveStats.value.timesUsed
  return `${n} time${n === 1 ? '' : 's'}`
})

watch(() => props.dish.name, (val) => { name.value = val })

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
  dishesStore.populateDishes()
}

async function doMove() {
  if (!moveToDish.value) return
  await dinnerRepo.moveDinnerDishes(appStore.activeFamilyId, props.dish.id, moveToDish.value.id)
  moveDialog.value = false
  emit('menuitem:moved')
}
</script>
