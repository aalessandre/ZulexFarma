export const LIST_BASIC_HTML_SNIPPET = `  <mat-list role="list">
                <mat-list-item role="listitem">Item 1</mat-list-item>
                <mat-list-item role="listitem">Item 2</mat-list-item>
                <mat-list-item role="listitem">Item 3</mat-list-item>
              </mat-list>
`;

export const LIST_TWOLINE_HTML_SNIPPET = `  <mat-list>
                <mat-list-item>
                  <span matListItemTitle class="text-base font-semibold">Title</span>
                  <span matListItemLine class="text-sm">Second line</span>
                </mat-list-item>
                <mat-list-item>
                  <span matListItemTitle class="text-base font-semibold">Title</span>
                  <span matListItemLine class="text-sm">Second line</span>
                </mat-list-item>
              </mat-list>
`;

export const LIST_THREELINE_HTML_SNIPPET = `  <mat-list>
                <mat-list-item>
                  <span matListItemTitle class="text-base font-semibold">Title</span>
                  <span matListItemLine class="text-sm">Second line</span>
                  <span matListItemLine class="text-sm">Third line</span>
                </mat-list-item>
                <mat-list-item>
                  <span matListItemTitle class="text-base font-semibold">Title</span>
                  <span matListItemLine class="text-sm"
                    >Second line. This line will truncate.</span
                  >
                  <span class="text-sm">Third line</span>
                </mat-list-item>
              </mat-list>
`;


export const LIST_THREELINE_TEXT_WRAPPING_HTML_SNIPPET = `  <mat-list style="max-width: 500px">
              <mat-list-item lines="3">
                <span matListItemTitle class="text-base font-semibold">Title</span>
                <span class="text-sm"
                  >Secondary line that will wrap because the list lines is
                  explicitly set to 3 lines. Text inside of a 'matListItemTitle'
                  or 'matListItemLine' will never wrap.
                </span>
              </mat-list-item>
              <mat-list-item lines="3" class="text-sm">
                <span matListItemTitle class="text-base font-semibold">Title</span>
                <span class="text-sm"
                  >Secondary line that will wrap because the list lines is
                  explicitly set to 3 lines. Text inside of a 'matListItemTitle'
                  or 'matListItemLine' will never wrap.
                </span>
              </mat-list-item>
            </mat-list>
`;

export const LIST_SELECTION_HTML_SNIPPET = `  <mat-selection-list #shoes>
                @for(shoe of typesOfShoes; track shoe) {
                <mat-list-option>
                  {{ shoe }}
                </mat-list-option>
                }
              </mat-selection-list>
  
              <p class="text-sm font-semibold p-4 bg-light-primary rounded-md">
                Options selected: {{ shoes.selectedOptions.selected.length }}
              </p>
`;

export const LIST_SINGLE_SELECTION_HTML_SNIPPET = `  <mat-selection-list #shoes2 [multiple]="false">
                @for(shoe of typesOfShoes; track shoe) {
                <mat-list-option [value]="shoe">
                  {{ shoe }}
                </mat-list-option>
                }
              </mat-selection-list>
  
              <p class="text-sm font-semibold p-4 bg-light-primary rounded-md">
                Option selected:
                {{
                  shoes2.selectedOptions.hasValue()
                    ? shoes2.selectedOptions.selected[0].value
                    : "None"
                }}
              </p>
`;

export const LIST_SECTIONS_HTML_SNIPPET = `  <mat-list>
                <div mat-subheader class="text-base mb-4 font-semibold">Folders</div>
                @for(folder of folders; track folder.name) {
                <mat-list-item>
                  <mat-icon matListItemIcon>folder</mat-icon>
                  <div matListItemTitle class="text-base font-semibold">
                    {{ folder.name }}
                  </div>
                  <div matListItemLine class="text-sm">
                    {{ folder.updated | date }}
                  </div>
                </mat-list-item>
                }
  
                <mat-divider></mat-divider>
                <div mat-subheader class="text-base my-4 font-semibold">Notes</div>
                @for(note of notes; track note.name) {
                <mat-list-item>
                  <mat-icon matListItemIcon>note</mat-icon>
                  <div matListItemTitle class="text-base font-semibold">
                    {{ note.name }}
                  </div>
                  <div matListItemLine class="text-sm">
                    {{ note.updated | date }}
                  </div>
                </mat-list-item>
                }
              </mat-list>
`;