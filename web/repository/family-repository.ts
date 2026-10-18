import type { Family, FamilySelect } from '~/types'
import { grocerySelectionSchema, groceryStoreSchema } from '~/types/grocery-offers'
import { z } from 'zod'

type ApiFetch = <T>(path: string, options?: Parameters<typeof $fetch>[1]) => Promise<T>
type ApiFetchRaw = (path: string, options?: RequestInit) => Promise<{ status: number; data: unknown }>

export class FamilyRepository {
  constructor(
    private apiFetch: ApiFetch,
    private apiFetchRaw: ApiFetchRaw,
  ) {}

  async all() {
    return (await this.apiFetch<Family[]>('/api/families/')).map(family => this.withGroceryStores(family))
  }

  async get(familyId: string) {
    return this.withGroceryStores(await this.apiFetch<Family>(`/api/families/${familyId}`))
  }

  private withGroceryStores(family: Family): Family {
    return { ...family, ...grocerySelectionSchema.parse({ groceryStores: family.groceryStores ?? [] }),
      supportedGroceryStores: z.array(groceryStoreSchema).parse(family.supportedGroceryStores ?? []) }
  }

  async saveGroceryStores(familyId: string, storeIds: string[]) {
    const response = await this.apiFetch<unknown>(`/api/families/${familyId}/grocery-stores`, { method: 'PUT', body: { storeIds } })
    return grocerySelectionSchema.parse(response).groceryStores
  }

  familySelectors() {
    return this.apiFetch<FamilySelect[]>('/api/families/select')
  }

  async inviteFamilyMember(familyId: string, email: string): Promise<boolean> {
    // 200 = user found and invited, 204 = user not found, 401 = not authorized
    const { status } = await this.apiFetchRaw(`/api/family/${familyId}/member`, {
      method: 'POST',
      body: JSON.stringify({ email }),
    })
    if (status === 401) throw new Error('Unauthorized')
    return status === 200
  }

  async createFamily(familyName: string): Promise<boolean> {
    const { status } = await this.apiFetchRaw('/api/families', {
      method: 'POST',
      body: JSON.stringify({ name: familyName }),
    })
    return status === 200 || status === 204
  }

  createFamilyMember(familyId: string, name: string) {
    return this.apiFetch(`/api/families/${familyId}/member/noautonomy`, {
      method: 'POST',
      body: { name },
    })
  }

  async mergeNonAutonomousMember(familyId: string, nonAutonomousMemberId: string, autonomousMemberId: string): Promise<boolean> {
    const { status } = await this.apiFetchRaw(
      `/api/families/${familyId}/member/${nonAutonomousMemberId}/merge`,
      { method: 'POST', body: JSON.stringify({ autonomousMemberId }) },
    )
    return status === 200
  }

  async setMemberRole(familyId: string, memberId: string, isOwner: boolean): Promise<void> {
    const { status } = await this.apiFetchRaw(
      `/api/families/${familyId}/member/${memberId}/role`,
      { method: 'PUT', body: JSON.stringify({ isOwner }) },
    )
    if (status === 401) throw new Error('Unauthorized')
    if (status === 400) throw new Error('INVALID_ROLE_CHANGE')
    if (status !== 200) throw new Error('UNEXPECTED_ERROR')
  }
}
