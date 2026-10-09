import { myInventoryQueryOptions } from "@/src/entities/inventory";
import { useQuery } from "@tanstack/react-query";

export function useMyInventory() {
  const { data, isPending, error } = useQuery(myInventoryQueryOptions());

  return {
    inventory: data ?? null,
    isPending,
    error,
  };
}
