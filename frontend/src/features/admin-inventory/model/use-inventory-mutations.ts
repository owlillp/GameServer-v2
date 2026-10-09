import {
  inventoryApi,
  inventoryQueryKeys,
  type AddCarRequest,
  type GrantItemsRequest,
  type Inventory,
  type RevokeItemsRequest,
} from "@/src/entities/inventory";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";

export type InventoryAction =
  | { type: "grant"; request: GrantItemsRequest }
  | { type: "revoke"; request: RevokeItemsRequest }
  | { type: "equip"; slot: string; itemId: string }
  | { type: "unequip"; slot: string }
  | { type: "addCar"; request: AddCarRequest }
  | { type: "removeCar"; carId: string }
  | { type: "installPart"; carId: string; slot: string; partItemId: string }
  | { type: "removePart"; carId: string; slot: string };

const SUCCESS_MESSAGE: Record<InventoryAction["type"], string> = {
  grant: "Предметы выданы",
  revoke: "Предметы отозваны",
  equip: "Предмет надет",
  unequip: "Предмет снят",
  addCar: "Машина добавлена",
  removeCar: "Машина удалена",
  installPart: "Деталь установлена",
  removePart: "Деталь снята",
};

export function useInventoryMutations(accountId: string | null) {
  const queryClient = useQueryClient();

  const mutation = useMutation({
    mutationFn: (action: InventoryAction): Promise<Inventory> => {
      if (!accountId) {
        throw new Error("Account is not selected");
      }

      switch (action.type) {
        case "grant":
          return inventoryApi.adminGrant(accountId, action.request);
        case "revoke":
          return inventoryApi.adminRevoke(accountId, action.request);
        case "equip":
          return inventoryApi.adminEquip(accountId, action.slot, action.itemId);
        case "unequip":
          return inventoryApi.adminUnequip(accountId, action.slot);
        case "addCar":
          return inventoryApi.adminAddCar(accountId, action.request);
        case "removeCar":
          return inventoryApi.adminRemoveCar(accountId, action.carId);
        case "installPart":
          return inventoryApi.adminInstallPart(
            accountId,
            action.carId,
            action.slot,
            action.partItemId,
          );
        case "removePart":
          return inventoryApi.adminRemovePart(
            accountId,
            action.carId,
            action.slot,
          );
      }
    },
    onSuccess: (_data, action) => {
      if (accountId) {
        void queryClient.invalidateQueries({
          queryKey: inventoryQueryKeys.admin(accountId),
        });
      }
      toast.success(SUCCESS_MESSAGE[action.type]);
    },
  });

  return {
    run: mutation.mutateAsync,
    isPending: mutation.isPending,
    error: mutation.error,
    reset: mutation.reset,
  };
}
