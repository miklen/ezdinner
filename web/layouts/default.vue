<template>
  <v-app id="ezdinner">
    <TopbarLarge v-if="!smAndDown" />
    <TopbarSmall v-else :links="links" />

    <v-main class="bg-grey-lighten-3">
      <v-container :fluid="md">
        <v-row>
          <v-col v-if="!smAndDown" cols="2">
            <v-list nav color="transparent">
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
  </v-app>
</template>

<script setup lang="ts">
const { smAndDown, md } = useDisplay()
const appStore = useAppStore()
const familiesStore = useFamiliesStore()

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
