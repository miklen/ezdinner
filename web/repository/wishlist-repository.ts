import type { WishlistItem, AddWishRequest } from '~/types'

type ApiFetch = <T>(path: string, options?: Parameters<typeof $fetch>[1]) => Promise<T>
type ApiFetchRaw = (path: string, options?: RequestInit) => Promise<{ status: number; data: unknown }>

export class WishlistRepository {
  constructor(
    private apiFetch: ApiFetch,
    private apiFetchRaw: ApiFetchRaw,
  ) {}

  getWishlist(familyId: string): Promise<WishlistItem[]> {
    return this.apiFetch<WishlistItem[]>(`/api/families/${familyId}/wishlist`)
  }

  async addWish(
    familyId: string,
    request: AddWishRequest,
  ): Promise<{ wishId: string; alreadyExists: boolean }> {
    const { status, data } = await this.apiFetchRaw(`/api/families/${familyId}/wishlist`, {
      method: 'POST',
      body: JSON.stringify(request),
    })
    const body = data as { wishId: string }
    return { wishId: body.wishId, alreadyExists: status === 409 }
  }

  async upvoteWish(familyId: string, wishId: string): Promise<{ alreadyVoted: boolean }> {
    const { status } = await this.apiFetchRaw(
      `/api/families/${familyId}/wishlist/${wishId}/upvote`,
      { method: 'POST' },
    )
    return { alreadyVoted: status === 409 }
  }

  removeWish(familyId: string, wishId: string): Promise<void> {
    return this.apiFetch(`/api/families/${familyId}/wishlist/${wishId}`, { method: 'DELETE' })
  }
}
