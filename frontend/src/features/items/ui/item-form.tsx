"use client";

import type { Item } from "@/src/entities/items";
import { ApiErrorList } from "@/src/shared/ui/api-error-list";
import { Button } from "@/src/shared/ui/button";
import { Input } from "@/src/shared/ui/input";
import { Textarea } from "@/src/shared/ui/textarea";
import { zodResolver } from "@hookform/resolvers/zod";
import { useForm } from "react-hook-form";
import { z } from "zod";
import { useCreateItem, useUpdateItem } from "../model/use-items";

// Ограничения зеркалят TemplateService.Domain.ItemConstants.
const itemSchema = z.object({
  name: z
    .string()
    .min(1, "Название обязательно")
    .max(100, "Не более 100 символов"),
  description: z.string().max(500, "Не более 500 символов").optional(),
});

type ItemValues = z.infer<typeof itemSchema>;

type Props = {
  item?: Item;
  onSaved?: (item: Item) => void;
  onCancel?: () => void;
};

export function ItemForm({ item, onSaved, onCancel }: Props) {
  const isEdit = Boolean(item);

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<ItemValues>({
    resolver: zodResolver(itemSchema),
    mode: "onTouched",
    defaultValues: {
      name: item?.name ?? "",
      description: item?.description ?? "",
    },
  });

  const {
    createItem,
    isPending: isCreating,
    error: createError,
  } = useCreateItem();
  const {
    updateItem,
    isPending: isUpdating,
    error: updateError,
  } = useUpdateItem();

  const isPending = isCreating || isUpdating;
  const error = createError ?? updateError;

  const onSubmit = handleSubmit(async (values) => {
    try {
      const request = {
        name: values.name,
        description: values.description?.trim() || null,
      };

      const saved =
        isEdit && item
          ? await updateItem({ itemId: item.id, request })
          : await createItem(request);

      reset();
      onSaved?.(saved);
    } catch {
      // Ошибка уже в error — покажем ниже.
    }
  });

  return (
    <form
      onSubmit={onSubmit}
      noValidate
      className="space-y-4 rounded-lg border border-slate-200 bg-white p-5"
    >
      <h2 className="text-sm font-semibold text-slate-900">
        {isEdit ? "Изменить запись" : "Новая запись"}
      </h2>

      <div>
        <label
          htmlFor="item-name"
          className="mb-1 block text-sm font-medium text-slate-700"
        >
          Название
        </label>
        <Input
          id="item-name"
          autoComplete="off"
          maxLength={100}
          required
          enterKeyHint="next"
          {...register("name")}
        />
        {errors.name && (
          <p className="mt-1 text-xs text-red-600">{errors.name.message}</p>
        )}
      </div>

      <div>
        <label
          htmlFor="item-description"
          className="mb-1 block text-sm font-medium text-slate-700"
        >
          Описание
        </label>
        <Textarea
          id="item-description"
          rows={3}
          maxLength={500}
          placeholder="Короткое описание"
          {...register("description")}
        />
        {errors.description && (
          <p className="mt-1 text-xs text-red-600">
            {errors.description.message}
          </p>
        )}
      </div>

      <div className="flex items-center gap-3">
        <Button type="submit" disabled={isPending}>
          {isPending
            ? "Сохраняем..."
            : isEdit
              ? "Сохранить изменения"
              : "Создать запись"}
        </Button>
        {onCancel && (
          <Button type="button" variant="ghost" onClick={onCancel}>
            Отмена
          </Button>
        )}
      </div>

      <ApiErrorList
        error={error}
        fallback={
          isEdit ? "Не удалось обновить запись" : "Не удалось создать запись"
        }
      />
    </form>
  );
}
