
import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { IconModule } from 'src/app/icon/icon.module';
import { MaterialModule } from 'src/app/material.module';

interface productcards {
  id: number;
  imgSrc: string;
  title: string;
  price: string;
  rprice: string;
  date: string;
}

@Component({
  selector: 'app-gallery',
  imports: [MaterialModule, IconModule, FormsModule],
  templateUrl: './gallery.component.html',
})
export class GalleryComponent implements OnInit {

  searchText: string = '';

  filteredCards: productcards[] = [];

  productcards: productcards[] = [
    { id: 1, imgSrc: 'assets/images/products/s1.jpg', title: 'Boat Headphone', price: '285', rprice: '375', date: 'Tue, Apr 03, 2026' },
    { id: 2, imgSrc: 'assets/images/products/s2.jpg', title: 'MacBook Air Pro', price: '285', rprice: '375', date: 'Tue, Apr 10, 2026' },
    { id: 3, imgSrc: 'assets/images/products/s3.jpg', title: 'Red Velvet Dress', price: '285', rprice: '375', date: 'Tue, Apr 15, 2026' },
    { id: 4, imgSrc: 'assets/images/products/s4.jpg', title: 'Soft Plush Teddy', price: '285', rprice: '375', date: 'Tue, Apr 12, 2026' },
    { id: 5, imgSrc: 'assets/images/products/s5.jpg', title: 'Boat Bass Booster', price: '285', rprice: '375', date: 'Tue, Apr 14, 2026' },
    { id: 6, imgSrc: 'assets/images/products/s6.jpg', title: 'MacBook Ultra Slim', price: '285', rprice: '375', date: 'Tue, Apr 18, 2026' },
    { id: 7, imgSrc: 'assets/images/products/s7.jpg', title: 'Crimson Party Dress', price: '285', rprice: '375', date: 'Tue, Apr 20, 2026' },
    { id: 8, imgSrc: 'assets/images/products/s8.jpg', title: 'Cuddly Teddy Gift', price: '285', rprice: '375', date: 'Tue, Apr 22, 2026' },
    { id: 9, imgSrc: 'assets/images/products/s9.jpg', title: 'Boat Sonic Headset', price: '285', rprice: '375', date: 'Tue, Apr 25, 2026' },
    { id: 10, imgSrc: 'assets/images/products/s10.jpg', title: 'MacBook Pro 2026', price: '285', rprice: '375', date: 'Tue, Apr 27, 2026' },
    { id: 11, imgSrc: 'assets/images/products/s1.jpg', title: 'Evening Gown - Red', price: '285', rprice: '375', date: 'Tue, Apr 29, 2026' },
    { id: 12, imgSrc: 'assets/images/products/s2.jpg', title: 'Fluffy Bear Surprise', price: '285', rprice: '375', date: 'Tue, Apr 30, 2026' },
    { id: 13, imgSrc: 'assets/images/products/s2.jpg', title: 'Boat Audio Pro', price: '285', rprice: '375', date: 'Tue, Apr 03, 2026' },
    { id: 14, imgSrc: 'assets/images/products/s3.jpg', title: 'MacBook Studio Edition', price: '285', rprice: '375', date: 'Tue, Apr 10, 2026' },
    { id: 15, imgSrc: 'assets/images/products/s4.jpg', title: 'Ruby Cocktail Dress', price: '285', rprice: '375', date: 'Tue, Apr 15, 2026' },
    { id: 16, imgSrc: 'assets/images/products/s5.jpg', title: 'Tiny Bear Hug Toy', price: '285', rprice: '375', date: 'Tue, Apr 18, 2026' }
  ];

  ngOnInit(): void {
    this.filteredCards = [...this.productcards];
  }
  filterCards() {
    const text = this.searchText.toLowerCase();

    this.filteredCards = this.productcards.filter(card =>
      card.title.toLowerCase().includes(text) ||
      card.date.toLowerCase().includes(text)
    );
  }
}
