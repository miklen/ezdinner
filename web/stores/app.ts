const LS_KEY = 'ezdinner:activeFamilyId'

export const useAppStore = defineStore('app', () => {
  const activeFamilyId = ref<string>(
    import.meta.client ? (localStorage.getItem(LS_KEY) ?? '') : '',
  )

  function setActiveFamilyId(id: string) {
    activeFamilyId.value = id
    if (import.meta.client) localStorage.setItem(LS_KEY, id)
  }

  return { activeFamilyId, setActiveFamilyId }
})
