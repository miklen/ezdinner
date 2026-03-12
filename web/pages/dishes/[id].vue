<template>
  <span>
    <v-row style="padding-bottom: 16px">
      <!-- main content -->
      <v-col cols="12" md="8">
        <v-row>
          <v-col>
            <DishCard v-if="dish" :dish="dish" :dish-stats="dish.dishStats" />
          </v-col>
        </v-row>
        <v-row>
          <v-col>
            <v-card>
              <v-card-title>
                Recipe &amp; notes
                <v-spacer />
                <v-card-actions v-if="editNotesMode">
                  <v-btn variant="text" @click="disableEditNotesMode">Cancel</v-btn>
                  <v-btn variant="text" color="primary" @click="doUpdateNotes">Save</v-btn>
                </v-card-actions>
                <v-card-actions v-else>
                  <v-btn variant="text" color="primary" @click="enableEditNotesMode">Edit</v-btn>
                </v-card-actions>
              </v-card-title>
              <v-card-text v-if="url || editNotesMode">
                <v-row>
                  <v-col style="display: flex; align-items: center; gap: 8px">
                    <v-icon>mdi-link-variant</v-icon>
                    <v-text-field v-if="editNotesMode" v-model="url" density="compact" />
                    <a v-else :href="url" target="_blank">{{ url }}</a>
                  </v-col>
                </v-row>
              </v-card-text>
              <div class="notes-area pa-4">
                <div v-if="editNotesMode">
                  <textarea ref="textareaRef" v-model="notes" />
                </div>
                <!-- eslint-disable-next-line vue/no-v-html -->
                <span v-else v-html="notesHtml" />
              </div>
            </v-card>
          </v-col>
        </v-row>
      </v-col>

      <!-- side panel -->
      <v-col>
        <v-row>
          <v-col>
            <v-card>
              <v-card-title>Family rating</v-card-title>
              <v-card-text>
                <v-row v-for="member in familyMembers" :key="member.id">
                  <v-col style="display: flex; align-items: center">
                    <v-avatar :color="member.id === userId ? 'primary' : 'grey'" size="40" class="text-white">
                      {{ getInitials(member.name) }}
                    </v-avatar>
                    <v-rating
                      color="primary"
                      half-increments
                      empty-icon="mdi-heart-outline"
                      full-icon="mdi-heart"
                      half-icon="mdi-heart-half-full"
                      length="5"
                      size="25"
                      :model-value="ratings[member.id] ?? 0"
                      :readonly="member.id !== userId"
                      @update:model-value="(val) => updateRating(val, member.id)"
                    />
                  </v-col>
                </v-row>
              </v-card-text>
            </v-card>
          </v-col>
        </v-row>
        <v-row>
          <v-col>
            <v-card>
              <v-card-title>Dates</v-card-title>
              <v-card-subtitle v-if="dish">
                You last had {{ dish.name }} {{ daysAgo }} days ago
              </v-card-subtitle>
              <v-table>
                <thead>
                  <tr>
                    <th class="text-left">Date</th>
                    <th class="text-center">Days since previous</th>
                  </tr>
                </thead>
                <tbody>
                  <tr v-for="date in dates" :key="date.date">
                    <td>{{ formatDate(date.date) }}</td>
                    <td class="text-center">{{ date.daysSinceLast }}</td>
                  </tr>
                </tbody>
              </v-table>
            </v-card>
          </v-col>
        </v-row>
      </v-col>
    </v-row>
  </span>
</template>

<script setup lang="ts">
import { DateTime } from 'luxon'
import { marked } from 'marked'
import EasyMDE from 'easymde'
import 'easymde/dist/easymde.min.css'
import type { Dish, DinnerDate } from '~/types'

useHead({ title: 'Dish' })

const route = useRoute()
const appStore = useAppStore()
const familiesStore = useFamiliesStore()
const { dishes: dishRepo } = useRepositories()
const { $msal } = useNuxtApp()

const dish = ref<Dish | null>(null)
const url = ref('')
const notes = ref('')
const notesHtml = ref('')
const dates = ref<DinnerDate[]>([])
const editNotesMode = ref(false)
const mde = ref<EasyMDE | null>(null)
const textareaRef = ref<HTMLElement | null>(null)

const userId = computed(() => $msal.getObjectId())
const familyMembers = computed(() => familiesStore.activeFamily?.familyMembers ?? [])
const ratings = computed<Record<string, number>>(() =>
  Object.fromEntries(dish.value?.ratings?.map((r) => [r.familyMemberId, r.rating]) ?? []),
)
const daysAgo = computed(() => {
  if (!dish.value?.dishStats?.lastUsed) return 0
  return Math.floor(DateTime.now().diff(dish.value.dishStats.lastUsed, 'days').days)
})

async function init() {
  dish.value = await dishRepo.getFull(route.params.id as string, appStore.activeFamilyId)
  notes.value = dish.value.notes
  notesHtml.value = marked.parse(dish.value.notes || '') as string
  url.value = dish.value.url
  dates.value = dish.value.dates
}

async function updateRating(newRating: number, familyMemberId: string) {
  if (!dish.value) return
  await dishRepo.updateRating(dish.value.id, newRating, familyMemberId)
  await init()
}

async function doUpdateNotes() {
  if (!dish.value) return
  await dishRepo.updateNotes(dish.value.id, notes.value, url.value)
  disableEditNotesMode()
}

function enableEditNotesMode() {
  editNotesMode.value = true
  nextTick(() => {
    if (!textareaRef.value) return
    mde.value = new EasyMDE({ element: textareaRef.value, spellChecker: false })
    mde.value.codemirror.on('change', () => {
      notes.value = mde.value?.value() ?? ''
      notesHtml.value = marked.parse(notes.value) as string
    })
  })
}

function disableEditNotesMode() {
  editNotesMode.value = false
  mde.value?.toTextArea()
  mde.value = null
}

function formatDate(date: string) {
  return DateTime.fromISO(date).toLocaleString(DateTime.DATE_FULL)
}

function getInitials(name: string) {
  const parts = name.split(' ')
  if (parts.length === 0) return '?'
  if (parts.length === 1) return parts[0][0].toUpperCase()
  return parts[0][0].toUpperCase() + parts[parts.length - 1][0].toUpperCase()
}

onMounted(init)
watch(() => appStore.activeFamilyId, () => navigateTo('/dishes'))
</script>

<style>
.notes-area {
  padding: 16px;
}
</style>
