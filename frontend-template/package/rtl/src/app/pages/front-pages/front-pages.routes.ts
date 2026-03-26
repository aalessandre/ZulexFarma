import { Routes } from "@angular/router";
import { HomePageComponent } from "./home-page/home-page.component";
import { HomePageDetailsComponent } from "./home-page-details/home-page-details.component";
import { AboutUsComponent } from "./about-us/about-us.component";
import { BlogComponent } from "./blog/blog.component";
import { ContactComponent } from "./contact/contact.component";
import { PortfolioComponent } from "./portfolio/portfolio.component";
import { PricingComponent } from "./pricing/pricing.component";
import { BlogDetailsComponent } from "./blog-details/blog-details.component";
import { PricePageComponent } from "./price-page/price-page.component";


export const FrontPagesRoutes: Routes = [
    
    {
        path: '',
        component: HomePageComponent,
        children: [
            { path: '', redirectTo: 'homepage', pathMatch: 'full' },
            {
                path:'homepage',component:HomePageDetailsComponent
            },
            {
                path:'about',component:AboutUsComponent 
            },
            {path:'blog',component:BlogComponent },
            { path: 'portfolio', component: PortfolioComponent },
            { path: 'pricing', component:PricingComponent   },
            { path: 'contact', component: ContactComponent },
           {path:'blog-details',component:BlogDetailsComponent}
        ]
      },
];