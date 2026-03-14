<template>
  <span>
    <v-row>
      <v-col cols="9" md="4">
        <v-text-field v-model="searchDish" label="Search dishes" />
      </v-col>
      <v-col class="text-right">
        <v-btn color="primary" icon="mdi-plus" @click="newDishDialog = true" />
      </v-col>
    </v-row>

    <v-row>
      <v-col cols="4" sm="3" xl="2">
        <v-select
          v-model="sorter"
          :items="sorting"
          label="Sort by"
          item-title="name"
          return-object
          density="compact"
          variant="solo"
        />
      </v-col>
    </v-row>

    <v-row>
      <v-col v-for="dish in dishes" :key="dish.id" cols="12" md="6" lg="4">
        <DishCard :dish="dish" :dish-stats="stats[dish.id]" @menuitem:moved="loadStats" />
      </v-col>
    </v-row>

    <v-dialog v-model="newDishDialog">
      <v-card>
        <v-card-title class="text-h5">Create dish</v-card-title>
        <v-divider />
        <v-card-text>
          <v-text-field v-model="newDishName" label="Dish name" @keyup.enter="createDish" />
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn variant="text" @click="newDishDialog = false">Cancel</v-btn>
          <v-btn variant="text" color="primary" @click="createDish">Create</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>
  </span>
</template>

<script setup lang="ts">
import type { Dish, DishStats } from '~/types'

useHead({ title: 'Dishes' })

const appStore = useAppStore()
const dishesStore = useDishesStore()
const { dishes: dishRepo } = useRepositories()

const searchDish = ref('')
const stats = ref<Record<string, DishStats>>({})
const newDishName = ref('')
const newDishDialog = ref(false)

type Sorter = { name: string; sorter: (a: Dish, b: Dish) => number }

const sorting = computed<Sorter[]>(() => [
  { name: '⬆A-Z', sorter: (a, b) => a.name.localeCompare(b.name) },
  { name: '⬇Z-A', sorter: (a, b) => b.name.localeCompare(a.name) },
  { name: '⬆❤', sorter: (a, b) => b.rating - a.rating },
  { name: '⬇❤', sorter: (a, b) => a.rating - b.rating },
  { name: '⬆Times', sorter: (a, b) => (stats.value[b.id]?.timesUsed ?? 0) - (stats.value[a.id]?.timesUsed ?? 0) },
  { name: '⬇Times', sorter: (a, b) => (stats.value[a.id]?.timesUsed ?? 0) - (stats.value[b.id]?.timesUsed ?? 0) },
  { name: '⬆Date', sorter: (a, b) => (stats.value[a.id]?.lastUsed?.toMillis() ?? Number.MIN_SAFE_INTEGER) - (stats.value[b.id]?.lastUsed?.toMillis() ?? Number.MIN_SAFE_INTEGER) },
  { name: '⬇Date', sorter: (a, b) => (stats.value[b.id]?.lastUsed?.toMillis() ?? Number.MAX_SAFE_INTEGER) - (stats.value[a.id]?.lastUsed?.toMillis() ?? Number.MAX_SAFE_INTEGER) },
])

const sorter = ref<Sorter>(sorting.value[0])

const dishes = computed(() =>
  [...dishesStore.dishes]
    .sort(sorter.value.sorter)
    .filter((d) => d.name?.toLowerCase().includes(searchDish.value.toLowerCase())),
)

async function loadStats() {
  stats.value = await dishRepo.allUsageStats(appStore.activeFamilyId)
}

async function init() {
  await dishesStore.populateDishes()
  await loadStats()
}

async function createDish() {
  await dishRepo.create(appStore.activeFamilyId, newDishName.value)
  newDishName.value = ''
  newDishDialog.value = false
  await init()
}

onMounted(init)
watch(() => appStore.activeFamilyId, (val) => { if (val) { stats.value = {}; init() } })
</script>
