import type { Family, FamilySelect } from '~/types'

type ApiFetch = <T>(path: string, options?: Parameters<typeof $fetch>[1]) => Promise<T>
type ApiFetchRaw = (path: string, options?: RequestInit) => Promise<{ status: number; data: unknown }>

export class FamilyRepository {
  constructor(
    private apiFetch: ApiFetch,
    private apiFetchRaw: ApiFetchRaw,
  ) {}

  all() {
    return this.apiFetch<Family[]>('/api/families/')
  }

  get(familyId: string) {
    return this.apiFetch<Family>(`/api/families/${familyId}`)
  }

  familySelectors() {
    return this.apiFetch<FamilySelect[]>('/api/families/select')
  }

  async inviteFamilyMember(familyId: string, email: string): Promise<boolean> {
    // 200 = user found and invited, 204 = user not found
    const { status } = await this.apiFetchRaw(`/api/family/${familyId}/member`, {
      method: 'POST',
      body: JSON.stringify({ email }),
    })
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
}
