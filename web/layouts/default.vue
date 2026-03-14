<template>
  <v-app id="ezdinner">
    <TopbarLarge v-if="!smAndDown" />
    <TopbarSmall v-else :links="links" />

    <v-main style="background-color: var(--color-background);">
      <v-container :fluid="md">
        <v-row>
          <v-col v-if="!smAndDown" cols="2">
            <v-list nav bg-color="transparent">
              <v-list-item
                v-for="item in links"
                :key="item.to"
                :prepend-icon="item.icon"
                :title="item.title"
                :to="item.to"
                exact
              />
            </v-list>
          </v-col>
          <v-col>
            <slot />
          </v-col>
        </v-row>
      </v-container>
    </v-main>

    <v-overlay :model-value="loading" class="align-center justify-center">
      <v-progress-circular size="50" color="primary" indeterminate />
    </v-overlay>

    <v-snackbar
      v-model="snackbar.visible.value"
      :color="snackbar.color.value"
      :timeout="snackbar.timeout.value"
      location="bottom"
      rounded="lg"
    >
      {{ snackbar.message.value }}
      <template #actions>
        <v-btn variant="text" @click="snackbar.visible.value = false">
          <v-icon>mdi-close</v-icon>
        </v-btn>
      </template>
    </v-snackbar>
  </v-app>
</template>

<script setup lang="ts">
const { smAndDown, md } = useDisplay()
const appStore = useAppStore()
const familiesStore = useFamiliesStore()
const snackbar = useSnackbar()

const loading = ref(true)

const links = computed(() => {
  const nav = [
    { icon: 'mdi-home', title: 'Home', to: '/home' },
    { icon: 'mdi-account-group', title: 'Families', to: '/families' },
  ]
  if (appStore.activeFamilyId) {
    nav.push(
      { icon: 'mdi-silverware-fork-knife', title: 'Dishes', to: '/dishes' },
      { icon: 'mdi-calendar-edit', title: 'Plan', to: '/plan' },
    )
  }
  return nav
})

onMounted(async () => {
  await familiesStore.getFamilySelectors()
  loading.value = false
  if (appStore.activeFamilyId) {
    familiesStore.getActiveFamily()
  }
})

watch(() => appStore.activeFamilyId, () => {
  familiesStore.getActiveFamily()
})
</script>
