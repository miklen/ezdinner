const LS_KEY = 'ezdinner:activeFamilyId'

export const useAppStore = defineStore('app', () => {
  const raw = import.meta.client ? (localStorage.getItem(LS_KEY) ?? '') : ''
  const activeFamilyId = ref<string>(raw.replace(/^"|"$/g, ''))

  function setActiveFamilyId(id: string) {
    activeFamilyId.value = id
    if (import.meta.client) localStorage.setItem(LS_KEY, id)
  }

  return { activeFamilyId, setActiveFamilyId }
})
