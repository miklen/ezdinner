import type { Family, FamilySelect } from '~/types'

export const useFamiliesStore = defineStore('families', () => {
  const appStore = useAppStore()
  const familySelectors = ref<FamilySelect[]>([])
  const activeFamily = ref<Family | null>(null)

  async function getFamilySelectors() {
    const { families } = useRepositories()
    familySelectors.value = await families.familySelectors()
    // Auto-select first family if none is active
    if (!appStore.activeFamilyId && familySelectors.value.length > 0) {
      appStore.setActiveFamilyId(familySelectors.value[0].id)
    }
  }

  async function getActiveFamily() {
    if (!appStore.activeFamilyId) return
    const { families } = useRepositories()
    const familyId = appStore.activeFamilyId
    const family = await families.get(familyId)
    if (familyId === appStore.activeFamilyId) activeFamily.value = family
  }

  function updateGroceryStores(familyId: string, storeIds: string[]) {
    if (activeFamily.value?.id === familyId) activeFamily.value = { ...activeFamily.value, groceryStores: [...storeIds] }
  }

  return { familySelectors, activeFamily, getFamilySelectors, getActiveFamily, updateGroceryStores }
})
