<template>
  <span class="topbar-profile">
    <span v-if="familyName" class="family-pill" @click="menu = !menu">
      <v-icon size="14" class="family-pill__icon">mdi-account-group</v-icon>
      <span class="family-pill__name">{{ familyName }}</span>
    </span>
    <v-menu v-model="menu" location="bottom end">
      <template #activator="{ props }">
        <button class="avatar-btn" v-bind="props" :aria-label="`User menu for ${fullName}`">
          <span class="avatar-btn__initials">{{ initials }}</span>
        </button>
      </template>
      <v-card min-width="220" rounded="lg">
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

const initials = computed(() => {
  if (!$msal.isAuthenticated.value) return ''
  return (
    ($msal.getFirstName()?.[0]?.toUpperCase() ?? '') +
    ($msal.getLastName()?.[0]?.toUpperCase() ?? '')
  )
})

const fullName = computed(() =>
  $msal.isAuthenticated.value ? `${$msal.getFirstName()} ${$msal.getLastName()}` : '',
)

const familyName = computed(() =>
  familiesStore.familySelectors.find((f) => f.id === appStore.activeFamilyId)?.name ?? '',
)
</script>

<style scoped>
.topbar-profile {
  display: flex;
  align-items: center;
  gap: var(--space-3);
}

.family-pill {
  display: flex;
  align-items: center;
  gap: var(--space-1);
  padding: var(--space-1) var(--space-3);
  border-radius: var(--radius-full);
  background-color: var(--color-surface-variant);
  border: 1px solid var(--color-border-medium);
  cursor: pointer;
  transition: background-color var(--duration-fast) var(--ease-out);
  min-height: 32px;
}

.family-pill:hover {
  background-color: var(--color-primary-light);
  border-color: var(--color-primary);
}

.family-pill__icon {
  color: var(--color-text-muted);
  flex-shrink: 0;
}

.family-pill__name {
  font-family: var(--font-body);
  font-size: var(--text-sm);
  font-weight: 500;
  color: var(--color-text-secondary);
  max-width: 120px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.avatar-btn {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 36px;
  height: 36px;
  border-radius: var(--radius-full);
  background-color: var(--color-primary);
  border: none;
  cursor: pointer;
  transition: background-color var(--duration-fast) var(--ease-out);
  flex-shrink: 0;
}

.avatar-btn:hover {
  background-color: var(--color-primary-dark);
}

.avatar-btn__initials {
  font-family: var(--font-body);
  font-size: 13px;
  font-weight: 600;
  letter-spacing: 0.5px;
  color: white;
}
</style>
