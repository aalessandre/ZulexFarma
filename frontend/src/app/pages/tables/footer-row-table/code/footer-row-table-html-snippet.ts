export const FOOTER_ROW_TABLE_HTML_SNIPPET = `  <div class="relative overflow-auto">
      <table mat-table [dataSource]="transactions" class="min-w-full">
        <!-- Item Column -->
        <ng-container matColumnDef="item">
          <th mat-header-cell *matHeaderCellDef class="font-semibold mat-subtitle-1 text-base pl-0">
            Item
          </th>
          <td mat-cell *matCellDef="let transaction" class="pl-0 whitespace-nowrap">
            <div class="flex items-center gap-3 ">
              <img [src]="transaction.img" alt="product" width="45" class="rounded-md" />
              <span class="text-base font-semibold">{{ transaction.item }} </span>
            </div>
          </td>
          <td mat-footer-cell *matFooterCellDef class="pl-0 font-semibold whitespace-nowrap">Total</td>
        </ng-container>

        <!-- Cost Column -->
        <ng-container matColumnDef="cost">
          <th mat-header-cell *matHeaderCellDef class="font-semibold mat-subtitle-1 text-base">
            Cost
          </th>
          <td mat-cell *matCellDef="let transaction" class="whitespace-nowrap">
            {{ transaction.cost | currency }}
          </td>
          <td mat-footer-cell *matFooterCellDef class="whitespace-nowrap">
            {{ getTotalCost() | currency }}
          </td>
        </ng-container>

        <tr mat-header-row *matHeaderRowDef="displayedColumns"></tr>
        <tr mat-row *matRowDef="let row; columns: displayedColumns"></tr>
        <tr mat-footer-row *matFooterRowDef="displayedColumns"></tr>
      </table>
    </div>
`;