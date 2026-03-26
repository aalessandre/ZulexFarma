import { Component, inject, ChangeDetectionStrategy } from '@angular/core';
import { TablerIconsModule } from 'angular-tabler-icons';
import { MaterialModule } from 'src/app/material.module';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';

@Component({
  selector: 'app-material-pro',
  imports: [
    TablerIconsModule,
    MaterialModule,
    MatButtonModule,

    MatDialogModule,
  ],
  templateUrl: './material-pro.component.html',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AppMaterialProComponent {
  constructor() {}

  readonly dialog = inject(MatDialog);

  openDialog() {
    const dialogRef = this.dialog.open(AppDialogContentExampleDialogComponent, {
      autoFocus: false,
    });

    dialogRef.afterClosed().subscribe((result) => {
      console.log(`Dialog result: ${result}`);
    });
  }
}

@Component({
  selector: 'dialog-content-example-dialog',
  templateUrl: 'dialog-content-example-dialog.html',
  standalone: true,
  imports: [MatDialogModule, MatButtonModule, TablerIconsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AppDialogContentExampleDialogComponent {}
