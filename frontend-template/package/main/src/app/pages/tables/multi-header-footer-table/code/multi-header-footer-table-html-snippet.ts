export const MULTI_HEADER_FOOTER_TABLE_HTML_SNIPPET = `  <div class="relative overflow-auto">
      <table mat-table [dataSource]="transactions" class="min-w-full">
        <!-- Item Column -->
        <ng-container matColumnDef="item">
          <th mat-header-cell *matHeaderCellDef class="font-semibold mat-subtitle-1 text-base border-b-0">
            Item
          </th>
          <td mat-cell *matCellDef="let transaction" class="whitespace-nowrap">
            <div class="flex items-center gap-3 ">
              <img [src]="transaction.img" alt="product" width="45" class="rounded-md" />
              <span class="text-base font-semibold">{{ transaction.item }} </span>
            </div>
          </td>
          <td mat-footer-cell *matFooterCellDef class="font-semibold mat-subtitle-1 text-base whitespace-nowrap">
            Total
          </td>
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

        <!-- Item Description Column -->
        <ng-container matColumnDef="item-description">
          <th mat-header-cell *matHeaderCellDef class="font-semibold mat-subtitle-1 text-sm">
            Name of the item purchased
          </th>
        </ng-container>

        <!-- Cost Description Column -->
        <ng-container matColumnDef="cost-description">
          <th mat-header-cell *matHeaderCellDef class="font-semibold mat-subtitle-1 text-sm">
            Cost of the item in USD
          </th>
        </ng-container>

        <!-- Disclaimer column -->
        <ng-container matColumnDef="disclaimer">
          <td mat-footer-cell *matFooterCellDef colspan="2" class="text-danger fw-medium whitespace-nowrap">
            Please note that the cost of items displayed are completely and
            totally made up.
          </td>
        </ng-container>

        <!-- The table will render two header rows, one data row per data object, and two footer rows. -->
        <tr mat-header-row *matHeaderRowDef="displayedColumns" class="example-first-header-row"></tr>
        <tr mat-header-row *matHeaderRowDef="['item-description', 'cost-description']"
          class="example-second-header-row italic"></tr>

        <tr mat-row *matRowDef="let row; columns: displayedColumns"></tr>

        <tr mat-footer-row *matFooterRowDef="displayedColumns" class="example-first-footer-row font-bold"></tr>
        <tr mat-footer-row *matFooterRowDef="['disclaimer']" class="example-second-footer-row"></tr>
      </table>
    </div>
`;