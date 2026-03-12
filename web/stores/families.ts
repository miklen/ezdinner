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
    activeFamily.value = await families.get(appStore.activeFamilyId)
  }

  return { familySelectors, activeFamily, getFamilySelectors, getActiveFamily }
})
