import type { AccountResponse } from "../accounts/AccountResponse"

export interface UserDto {
    id: string
    email: string
    name: string
    surname: string
    userRole: string
    currentAccount: AccountResponse
}