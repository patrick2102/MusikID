import React, { Component } from 'react';
import { Route } from 'react-router';
import { Layout } from './components/Layout';
import { Home } from './components/Home';
import { Files } from './components/Files';
import { Channels } from './components/Channels';
import { Tracks } from './components/Tracks';
import { OnDemandResults } from './components/OnDemandResults';
import { LivestreamResults } from './components/LivestreamResults';
import { CheckFiles } from './components/CheckFiles';

import './custom.css'

export default class App extends Component {
  static displayName = App.name;

  render () {
    return (
      <Layout>
         <Route exact path='/' component={Home} />
            <Route path='/jobs' component={Files} />
            <Route path='/channels' component={Channels} />
            <Route path='/tracks' component={Tracks} />
            <Route path='/onDemandFiles' component={OnDemandResults} />
            <Route path='/liveChannels' component={LivestreamResults} />
            <Route path='/trackFolderCheck' component={CheckFiles} />
      </Layout>
    );
  }
}
