<template>
  <v-app id="ezdinner">
    <!-- Desktop: top bar -->
    <TopbarLarge v-if="!smAndDown" />

    <!-- Mobile: minimal top bar (no hamburger — bottom nav handles navigation) -->
    <TopbarSmall v-else />

    <!-- Desktop: icon rail (replaces the old cols="2" nav column) -->
    <v-navigation-drawer
      v-if="!smAndDown"
      permanent
      rail
      :elevation="0"
      class="icon-rail"
    >
      <v-list nav class="icon-rail__list">
        <v-tooltip
          v-for="item in links"
          :key="item.to"
          :text="item.title"
          location="end"
        >
          <template #activator="{ props }">
            <v-list-item
              v-bind="props"
              :to="item.to"
              exact
              class="icon-rail__item"
            >
              <template #prepend>
                <v-icon>{{ route.path === item.to ? item.iconActive : item.icon }}</v-icon>
              </template>
            </v-list-item>
          </template>
        </v-tooltip>
      </v-list>
    </v-navigation-drawer>

    <v-main class="app-main">
      <v-container :fluid="md" class="app-container">
        <slot />
      </v-container>
    </v-main>

    <!-- Mobile: bottom navigation -->
    <BottomNav v-if="smAndDown" :links="links" />

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
const route = useRoute()

const loading = ref(true)

const links = computed(() => {
  const nav = [
    { icon: 'mdi-home-outline', iconActive: 'mdi-home', title: 'Home', to: '/home' },
    { icon: 'mdi-account-group-outline', iconActive: 'mdi-account-group', title: 'Families', to: '/families' },
  ]
  if (appStore.activeFamilyId) {
    nav.push(
      { icon: 'mdi-silverware-fork-knife', iconActive: 'mdi-silverware-fork-knife', title: 'Dishes', to: '/dishes' },
      { icon: 'mdi-calendar-blank-outline', iconActive: 'mdi-calendar-blank', title: 'Plan', to: '/plan' },
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

<style scoped>
.icon-rail {
  background-color: var(--color-surface) !important;
  border-right: 1px solid var(--color-border) !important;
}

.icon-rail__list {
  padding-top: var(--space-4);
}

.icon-rail__item {
  border-radius: var(--radius-md) !important;
  margin-bottom: var(--space-2);
  min-height: 48px !important;
}

/* Active state: terracotta icon */
.icon-rail__item.v-list-item--active {
  color: var(--color-primary) !important;
  background-color: rgba(212, 101, 42, 0.08) !important;
}

.app-main {
  background-color: var(--color-background) !important;
}

.app-container {
  padding-top: var(--space-6) !important;
  padding-bottom: var(--space-12) !important;
}
</style>
