<template>
  <span>
    <span class="family-label" @click="menu = !menu">{{ familyName }}</span>
    <v-menu v-model="menu" location="bottom end">
      <template #activator="{ props }">
        <v-avatar class="text-white" color="primary" size="36" v-bind="props">
          {{ initials }}
        </v-avatar>
      </template>
      <v-card min-width="220">
        <v-list>
          <v-list-item prepend-icon="mdi-account" :title="fullName" />
        </v-list>
        <v-divider />
        <FamilyListItems />
        <v-divider />
        <v-list>
          <v-list-item prepend-icon="mdi-logout" title="Sign out" @click="$msal.signOut()" />
        </v-list>
      </v-card>
    </v-menu>
  </span>
</template>

<script setup lang="ts">
const { $msal } = useNuxtApp()
const appStore = useAppStore()
const familiesStore = useFamiliesStore()

const menu = ref(false)

const initials = computed(() =>
  ($msal.getFirstName()?.[0]?.toUpperCase() ?? '') +
  ($msal.getLastName()?.[0]?.toUpperCase() ?? ''),
)

const fullName = computed(() => `${$msal.getFirstName()} ${$msal.getLastName()}`)

const familyName = computed(() =>
  familiesStore.familySelectors.find((f) => f.id === appStore.activeFamilyId)?.name ?? '',
)
</script>

<style scoped>
.family-label {
  cursor: pointer;
  margin-right: 8px;
}
</style>
