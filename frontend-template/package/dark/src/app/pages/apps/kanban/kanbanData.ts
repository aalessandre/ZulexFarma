import { Todos } from './kanban';

export const todos: Todos[] = [
  {
    id: 1,
    title: 'This is first task',
    description: '',
    date: '1 month ago',
    taskProperty: 'Design',
    imageUrl: '/assets/images/taskboard/kanban-img-1.jpg',
  },
  {
    id: 2,
    title: 'Lets do some task on pd',
    description: 'Lorem ipsum dolor sit amet, consectetur adipisicing elit, o eiusmod tempor incid',
    date: '2 days ago',
    taskProperty: 'Development',
    imageUrl: '',
  },
  {
    id: 3,
    title: 'Do some projects on React Native with Flutter',
    description: '',
    class: 'task-status-info',
    date: '4 days ago',
    taskProperty: 'Mobile',
    imageUrl: '',
  },
];

export const inprogress: Todos[] = [
  {
    id: 201,
    title: 'Work on Dashboard Designing',
    description: '',
    date: '4 seconds ago',
    taskProperty: 'Mobile',
    imageUrl: '',
  },
  {
    id: 202,
    title: 'Battle with fire',
    description:'',
    class: 'task-status-danger',
    date: '4 seconds ago',
    taskProperty: 'Design',
    imageUrl: '',
  },
  {
    id: 203,
    title: 'Do some projects on Reactjs with tailwind',
    description: '',
    date: '4 seconds ago',
    taskProperty: 'Mobile',
    imageUrl: '',
  },
];

export const completed: Todos[] = [
  {
    id: 301,
    title: 'Develop React app',
    description: '',
    date: '1 month ago',
    taskProperty: 'Mobile',
    imageUrl: '',
  },
  {
    id: 302,
    title: 'Do some task on Nuxtjs',
    description: 'Lorem ipsum dolor sit amet, consectetur adipisicing elit',
    class: 'task-status-success',
    date: '4 seconds ago',
    taskProperty: 'Design',
    imageUrl: '',
  },
];

export const onhold: Todos[] = [
  {
    id: 401,
    title: 'Create a Nextjs Dashboard',
    description: '',
    date: '4 seconds ago',
    taskProperty: 'Design',
    imageUrl: '',
  },
  {
    id: 402,
    title: 'Solve Vuejs Project errors',
    description: '',
    class: 'task-status-danger',
    date: '1 month ago',
    taskProperty: 'Development',
    imageUrl: '/assets/images/taskboard/kanban-img-2.jpg',
  },
  {
    id: 403,
    title: 'Redesign Overview',
    description: '',
    date: '4 seconds ago',
    taskProperty: 'Design',
    imageUrl: '',
  },
];
