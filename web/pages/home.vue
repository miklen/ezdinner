<template>
  <Content :split="!!appStore.activeFamilyId">
    <v-row>
      <v-col class="text-center">
        <h1>Welcome</h1>
        <span>
          Get started by creating a family. Add family members to participate in
          planning. And begin tracking and planning your meals.
        </span>
      </v-col>
    </v-row>

    <v-row v-if="appStore.activeFamilyId">
      <v-col cols="12" xl="6">
        <v-card>
          <v-card-title>Dinner tonight</v-card-title>
          <v-card-text v-if="dinners[0]?.menu.length">
            <v-list>
              <v-list-item v-for="item in dinners[0].menu" :key="item.dishId">
                <v-list-item-title>{{ item.dishName }}</v-list-item-title>
                <template #append>
                  <v-btn icon variant="text" @click="navigateTo('/dishes/' + item.dishId)">
                    <v-icon>mdi-information-outline</v-icon>
                  </v-btn>
                </template>
              </v-list-item>
            </v-list>
          </v-card-text>
          <v-card-text v-else>
            Nothing planned for tonight! Go to <NuxtLink to="/plan">Plan</NuxtLink> and get organized!
          </v-card-text>
        </v-card>
      </v-col>

      <v-col cols="12" xl="6">
        <v-card>
          <v-card-title>Up for tomorrow</v-card-title>
          <v-card-text v-if="dinners[1]?.menu.length">
            <v-list>
              <v-list-item v-for="item in dinners[1].menu" :key="item.dishId">
                <v-list-item-title>{{ item.dishName }}</v-list-item-title>
                <template #append>
                  <v-btn icon variant="text" @click="navigateTo('/dishes/' + item.dishId)">
                    <v-icon>mdi-information-outline</v-icon>
                  </v-btn>
                </template>
              </v-list-item>
            </v-list>
          </v-card-text>
          <v-card-text v-else>
            Nothing planned for tomorrow! Go to <NuxtLink to="/plan">Plan</NuxtLink> and get organized!
          </v-card-text>
        </v-card>
      </v-col>
    </v-row>

    <template #support>
      <PlanTopDishes />
    </template>
  </Content>
</template>

<script setup lang="ts">
import { DateTime } from 'luxon'
import type { Dinner } from '~/types'

useHead({ title: 'Home' })

const appStore = useAppStore()
const dishesStore = useDishesStore()
const { dinners: dinnerRepo } = useRepositories()

const dinners = ref<Dinner[]>([])

async function init() {
  if (!appStore.activeFamilyId) return
  await dishesStore.populateDishes()
  const today = DateTime.now()
  type RawDinner = Omit<Dinner, 'date'> & { date: string }
  const result = await dinnerRepo.getRange(appStore.activeFamilyId, today, today.plus({ days: 1 }))
  dinners.value = (result as unknown as RawDinner[]).map((dinner) => ({
    ...dinner,
    date: DateTime.fromISO(dinner.date),
    menu: dinner.menu.map((item) => ({
      ...item,
      dishName: dishesStore.dishMap[item.dishId] ?? 'Dish not available',
    })),
  }))
}

onMounted(init)
watch(() => appStore.activeFamilyId, init)
</script>
