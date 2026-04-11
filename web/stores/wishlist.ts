import type { WishlistItem } from '~/types'

export const useWishlistStore = defineStore('wishlist', () => {
  const appStore = useAppStore()
  const wishes = ref<WishlistItem[]>([])

  async function fetchWishes() {
    const { wishlist } = useRepositories()
    wishes.value = await wishlist.getWishlist(appStore.activeFamilyId)
  }

  async function addWish(
    dishId: string,
    dishName: string,
  ): Promise<{ wishId: string; alreadyExists: boolean }> {
    const { wishlist } = useRepositories()
    const result = await wishlist.addWish(appStore.activeFamilyId, { dishId, dishName })
    await fetchWishes()
    return result
  }

  async function upvoteWish(wishId: string): Promise<{ alreadyVoted: boolean }> {
    const { wishlist } = useRepositories()
    const result = await wishlist.upvoteWish(appStore.activeFamilyId, wishId)
    if (!result.alreadyVoted) {
      await fetchWishes()
    }
    return result
  }

  async function removeUpvote(wishId: string) {
    const { wishlist } = useRepositories()
    await wishlist.removeUpvote(appStore.activeFamilyId, wishId)
    const wish = wishes.value.find((w) => w.wishId === wishId)
    if (wish) {
      wish.isVotedByCurrentUser = false
      wish.voteCount = Math.max(0, wish.voteCount - 1)
    }
  }

  async function removeWish(wishId: string) {
    const { wishlist } = useRepositories()
    await wishlist.removeWish(appStore.activeFamilyId, wishId)
    wishes.value = wishes.value.filter((w) => w.wishId !== wishId)
  }

  return { wishes, fetchWishes, addWish, upvoteWish, removeUpvote, removeWish }
})
